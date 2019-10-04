dot_cmd="/c/Program Files (x86)/Graphviz2.38/bin/dot.exe"
python_cmd="python"

"$python_cmd" dottify_compilat.py  | tee compilat.dot | "$dot_cmd"   -Tpdf  -o compilat.pdf && echo "compilat ok" 
"$python_cmd" dottify_eval.py      | tee eval.dot     | "$dot_cmd"   -Tpdf  -o eval.pdf     && echo "eval ok" 

sleep 0.3 ; echo -n three .. ; sleep 0.3 ; echo -n two .. ; sleep 0.3 ; echo one ; sleep 0.7 