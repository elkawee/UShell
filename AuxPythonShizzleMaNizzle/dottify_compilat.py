

import sys,os
import json


if len( sys.argv ) > 1 :
    lines = open( sys.argv[1] , "r").read().split("\n")
else :
    lines = open( "compilat.json" , "r").read().split("\n")

deser = []
for l in lines :
    if len(l) == 0 : continue
    deser.append ( json.loads ( l ))



# order of opcodes of course matters. (and graphviz might change them)
# atm , execution order is the order they appear in in the file ( file format will prob. change )
opcodes = [ o for o in deser if o.get("kind") == "opcode" ]
graph_opcodes = []
for opcode in opcodes :
    graph_opcodes.append (   "%s [shape=box label=\"%s\" ] "  % (opcode["id"] , opcode["name"] )  )

graph_opcode_order_edges = []
for o1 , o2 in zip( opcodes , opcodes[1:] ) :
    graph_opcode_order_edges.append ( "%s -> %s [weight=10 arrowhead=none]" % ( o1["id"] , o2["id"] ) )

subgraph_opcodes = "{ rank=same ; ops ; " + ";".join( [ x["id"] for x in opcodes ] ) + "}"



op_ch_edges = [o for o in deser if o.get("kind") == "OP>CH_edge" ]
graph_op_ch_edges = []

def isInEdge ( edge ) :
	return edge["fieldname"] in [ "CH_in" , "lhsCH" ]

style_i = 0
style_rotation = [ "solid","dashed","dotted" ]
for edge in op_ch_edges :
	fmt_base = "%s -> %s "
	dot_base = fmt_base % ( edge["id_from"] , edge["id_to"] )






	#in edges  BLUE
	if isInEdge( edge ) :
		dot_base = fmt_base % (  edge["id_from"] , edge["id_to"] )
		style    = "[color = blue, weight=5 ,  tailport = w ,  dir=back]"
	#out edges RED
	elif edge["fieldname"] == "CH_out" :
		style = "[color = red ,  tailport = s, headport = nw]"
	# other
	else :
		# dot_base = "%s - %s " % ( edge["id_from"] , edge["id_to"] )  # weren't there undirected edges ?
		style = "[color=darkgreen, weight=0 ,  headport=n , arrowhead=none , style=%s]" % style_rotation[ style_i ]
		style_i = (style_i + 1 ) % 3


	graph_op_ch_edges.append ( dot_base + "  " + style )

CHs = [o for o in deser if o.get("kind") == "CH" ]
"""
order CHs by how they appear as input ot opcodes
"""
def foo () :
	for opc in opcodes :
		for edge in op_ch_edges :
			if isInEdge(edge) and  edge["id_from"] == opc["id"] :
				for ch in CHs :
					if ch["id"] == edge["id_to"] :
						yield ch

ordered_CHs = [ x for x in  foo() ]
# print (  ordered_CHs )

graph_CH_order_dummy_edges = [ "%s -> %s [ style = invis ]" % (ch1["id"] , ch2["id"] ) for ch1 , ch2 in zip ( ordered_CHs , ordered_CHs[1:] ) ]


graph_ordered_CHs = []
for ch in ordered_CHs :
    fmt = "%s [label=\"%s\"]"
    dot = fmt % ( ch["id"] , ch["name"] )
    graph_ordered_CHs.append ( dot )

subgraph_ordered_CHs = "{ rank=same ; CHs ;" + ";".join( graph_ordered_CHs ) + "}\n"

graph_rest_CHs = []
for ch in [ x for x in CHs if not x in ordered_CHs] :
    fmt = "%s [label=\"%s\"]"
    dot = fmt % ( ch["id"] , ch["name"] )
    graph_rest_CHs.append ( dot )



names = [ o for o in deser if o["kind"] == "scope_edge" ]


subgraph_names = "\n{ rank=same ; names ; " + ";".join( [ n["name"] for n in names ] ) + "}\n"
graph_names_edges = [ "%s -> %s " % ( n["name"] , n["id_to"]  ) for n in names ]


dot =   """
digraph G {

rankdir=TB;
forcelabels=true;  # ... forgot why

ranksep = 2.5 ;

#edge [arrowhead=none];

ops -> CHs -> other -> names

"""

dot +=  "\n".join( graph_opcodes ) + "\n"

dot += subgraph_opcodes + "\n"

dot +=  "\n".join( graph_opcode_order_edges ) + "\n"



dot +=  "\n".join( graph_op_ch_edges )+ "\n"


dot += subgraph_ordered_CHs
dot +=  "\n # dummy edges to order CHs \n"

dot +=  "\n".join ( graph_CH_order_dummy_edges ) + "\n"
dot += "\n  { rank=same ; other ; \n" + "\n".join ( graph_rest_CHs ) + "}\n"
dot += subgraph_names
dot += "\n  " +  ";".join ( graph_names_edges )  + "\n"

dot +=  "} #digraph "

print ( dot )

# f.write ( dot )
# f.flush ()

# dot_cmd = '"c:\Program Files (x86)\Graphviz2.38\bin\dot.exe"'
# dot_cmd = 'c:\Program Files (x86)\Graphviz2.38\bin\dot.exe'

# print ( os.system ( dot_cmd + " output.dot -Tpdf -o out.pdf" )  )
