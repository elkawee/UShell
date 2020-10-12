import json



def label_encode( str ) :
    import urllib.parse # quick hack - for labels, i'd rather use c-string-literal escaping ...
    return urllib.parse.quote(str)

class KindFilter :  # kinda stupid ... but i need py3 practise

        def __init__ ( self , obj_list , kind ) :
                self.cache = []
                for o in obj_list :
                        if o["kind"] == kind :
                                self.cache.append ( o )
        def __iter__ ( self ):
                return iter(self.cache)
        def __getitem__ ( self , key ) :
                if type(key) == int :
                        return self.cache[key]
                elif type(key) == str :  # lookup id-field
                        return [ obj for obj in self.cache if obj["id"] == key ] [0]

import sys

if len( sys.argv ) > 1 :
    lines = open( sys.argv[1] , "r").read().split("\n")
else :
    lines = open( "eval.json" , "r").read().split("\n")

deser = []
for l in lines :
    if len(l) == 0 : continue
    deser.append ( json.loads ( l ))


CHs             = KindFilter( deser, "CH"            )
columns         = KindFilter( deser, "column"        )
CH_column_edges = KindFilter( deser, "ch_column_edge" )

# apparantly the "cluster"-prefix for the name is needed  ???

column_template = """
subgraph cluster{i} {{
label="{label}"
color=blue
node [shape=box]
{boxes}
}}
"""

box_template = ' {box_id} [label="{box_payload}"] '
cluster_i = 0
def dot_column ( colm_dict ) :                            # dict -> dot_string

        def incoming_CH () :
                """ pretty brittle, assumes exactly one incoming edge to exist and exactly one CH for that
                    by the way the json is currently produced this holds ... but  :)
                """
                edge = [ e for e in CH_column_edges if e["id_to"] == colm_dict["id"] ][0]
                return CHs[ edge["id_from"] ]

        global cluster_i
        box_strs = []
        for box in colm_dict["boxes"] :
                box_strs.append( box_template.format( box_id = box["id"] , box_payload= label_encode( box["payload"] ) ) )

        ch = incoming_CH()


        R = column_template.format ( i = cluster_i , label=ch["name"] , boxes = "\n".join(box_strs ) )

        cluster_i += 1
        return R

inter_box_edge_template = " {vrom} -> {to} "

def dot_inter_box_edges () :
        for colm in columns :
                for box in colm["boxes"]:
                        for pred_id in box["preds"] :
                                if pred_id != "null" :
                                        yield inter_box_edge_template.format( vrom = box["id"] , to = pred_id )



print(   """
digraph G {

rankdir=RL;
# forcelabels=true;  # ... forgot why

ranksep = 1 ;

""")

for colm           in columns               : print ( dot_column ( colm ))
for inter_box_edge in dot_inter_box_edges() : print ( inter_box_edge )

print ( "} # digraph G " )
