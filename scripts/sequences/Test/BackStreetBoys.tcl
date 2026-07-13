#Backstreet Boys
sq_camera follow 0 1
do_wait camera
sq_wait all
do_action rotate front 0
sq_actor find Zwerg 20 3
sq_pen set ZwergenFormation 0
sq_pen move ZwergenFormation {0 0 -1}
sq_pen form ZwergenFormation RowHorMi 3
sq_wait none
do_action rotate back 0
sq_wait all
do_action walk ZwergenFormation {1 ...}
sq_camera move 1 1 -0.1 -0.5
do_action rotate front 1
sq_color 1 Yellow
do_text "Hallo!" 1
sq_camera move 2 1 -0.5 0
do_action rotate front 2
sq_color 2 Red
do_text "Hallo!?" 2
sq_camera move 3 1 -0.1 0.5
do_action rotate front 3
sq_color 3 Green
do_text "Hallo?" 3
sq_camera follow 0 1.2
sq_wait none
do_action anim warmbutt {1 ...}
sq_wait 0
do_action rotate front 0
sq_wait all
do_action anim bowllose 0
do_action anim bowlwin all
do_action anim kicka all
do_action anim bowlwin all
do_wait

