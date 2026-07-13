sq_actor find Wuker 100 6 any
sq_pen set Z [Getobjpos Feuerstelle 0]
sq_pen setz Z 14.5
sq_pen move Z {5 0 0}
do_action beam Z 0
sq_pen set T0 [Getobjpos Info_Pos_Troll 1]
sq_pen set C T0
sq_pen move C {-13 0 0}
sq_camera fix C 1.2 -0.2 0.2
sq_pen sety T0 38.4
sq_pen move T0 {-3 0 -2}
sq_pen set T1 T0;sq_pen move T1 {1 0 -4}
sq_pen set T2 T0;sq_pen move T2 {2 0 1}
sq_pen set T3 T0;sq_pen move T3 {-1 0 -1}
sq_pen set T4 T0;sq_pen move T4 {-2 0 -2}
sq_pen set T5 T0;sq_pen move T5 {3 0 -3}
do_action beam T {1 ...}
do_action walk T {1 ...}
sq_pen set Z2 Z
sq_pen move Z2 {12 0 0}
sq_pen set W Z
sq_pen form W RowHorLe 6
sq_wait 0
sq_camera follow 0 1.0
do_action run Z2 0
do_action anim shock 0
sq_camera stop
sq_wait none
do_action flee Z 0
do_action walk W 1
do_action walk W 2
do_action walk W 3
do_action walk W 4
do_action walk W 5
do_action walk W 6
sq_pen set C Z2
sq_pen move C {3 0 0}
sq_camera fix C 0.8 -0.2 0.7
do_wait time 6.2
sq_camera fix Z2 1.2 -0.2 -0.7
#sq_camera follow 3 1.0
sq_wait all
do_wait
do_wait