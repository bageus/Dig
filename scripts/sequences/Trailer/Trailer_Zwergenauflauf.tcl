sq_actor find Zwerg 300 7 0
sq_pen set zwergenauflauf [Getobjpos Info_Pos_Troll 0]
sq_pen move zwergenauflauf {-8 0 0}
sq_pen form zwergenauflauf RowHorRi 7
do_action beam zwergenauflauf all
do_action rotate left all
do_wait time 3
sq_pen move zwergenauflauf {-14 0 0}
sq_pen set kamera zwergenauflauf
sq_camera fix kamera 1.0 -0.1 0.5
sq_pen set kamera zwergenauflauf
sq_pen move kamera {8 0 0}
sq_pen form zwergenauflauf RowHorLe 7
sq_wait all
do_tooltakeout Axt {1 3}
do_tooltakeout Spitzhacke {0 5}
do_tooltakeout Hammer {4}
sq_wait none
do_action walk zwergenauflauf all
do_wait time 2
do_action walk zwergenauflauf all
sq_camera move kamera 1 -0.1 0.5 0.06
do_wait time 19
sq_wait 6
do_action anim stummble 6
sq_wait all
do_action walk zwergenauflauf 6

do_wait