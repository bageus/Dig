#sq_actor find Zwerg 300 1 0
sq_pen set treten [Getobjpos Info_Pos_Troll 0]
sq_pen set kamera treten 
sq_camera fix kamera 1.0 -0.1 0.5
sq_pen move treten {-2 0 7}
do_action beam treten all
sq_pen move treten {2.5 0 -7}
sq_wait all
do_action walk treten all  
sq_wait all
do_action anim kontrol all
sq_wait all
do_action anim invent_b all
sq_wait all
do_action anim breathe all
sq_wait all
do_action anim kontrol all
sq_wait all
do_action anim invent_c all
sq_wait all
do_wait time 1
#do_action anim boo all
#sq_wait all
do_action anim warmhands all
sq_wait all
#do_action anim switch all
#sq_wait all
do_action anim kickmachine all
set_anim [obj_query 0 "-class Dampfhammer -limit 1"] dampfhammer.ani 0 2
do_wait time 1
do_action anim bowlwin all
sq_wait all
do_action rotate front 0
do_action anim jumpa all
sq_wait all
do_wait time 3
set_anim [obj_query 0 "-class Dampfhammer -limit 1"] dampfhammer.standard 0 2
sq_wait all
