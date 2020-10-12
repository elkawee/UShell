dot_cmd="/c/Program Files (x86)/Graphviz2.38/bin/dot.exe"
python_cmd="python"

"$python_cmd" dottify_compilat.py                                 | tee compilat.dot  | "$dot_cmd"   -Tpdf  -o compilat.pdf && echo "compilat ok"
"$python_cmd" dottify_compilat.py compilat_GreenCam_with_fan.json | tee compilat_GreenCam_with_fan.dot | "$dot_cmd"   -Tpdf  -o compilat_.pdf && echo "compilat ok"

"$python_cmd" dottify_eval.py                                     | tee eval.dot     | "$dot_cmd"   -Tpdf  -o eval.pdf     && echo "eval ok"
"$python_cmd" dottify_eval.py   eval_GreenCam_with_fan.json       | tee eval_GreenCam_with_fan.dot     | "$dot_cmd"   -Tpdf  -o eval_.pdf     && echo "eval ok"
