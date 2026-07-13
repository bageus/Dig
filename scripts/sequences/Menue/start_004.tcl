+sq_object delete all
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Start {2 0 0}
sq_object summon Zwerg P_Start
global dancergen;call_method [Object 0] Editor_Set_Info [list {name Fred} "gender [set dancergen [lindex {male female} [irandom 2]]]"]
call_method [Object 0] init
global dancergen;sq_object summon Dummy_Muetze_holz_[string map {female b male a} $dancergen] P_Start
link_obj [Object 1] [Object 0] 4
do_wait time 0.2
sq_wait none
sq_actor find Zwerg
do_wait time 0.1
sq_actor actionlist 0 {{anim discoc} loop}
do_action anim discoc 0
sq_actor eyes 0 {11}
set_roty [Object 0] 4.7
set_vel [Object 0] {0.5 0 0.0}
do_wait time 5
set_roty [Object 0] 4.0
set_vel [Object 0] {0.4 0 -0.8}
do_wait time 5
set_roty [Object 0] 4.7
set_vel [Object 0] {0.5 0 0}
do_wait time 8
#sq_actor actionlist 0 {{anim discoa} loop}
#sq_actor actionlist 0 {{anim discoc} loop}
set_vel [Object 0] {0.4 0 0.9}
set_roty [Object 0] 5.1
do_wait time 2
set_roty [Object 0] 5.4
do_wait
set_roty [Object 0] 5.7
do_wait
set_roty [Object 0] 6.0
do_wait
set_roty [Object 0] 0.0
do_wait
set_roty [Object 0] 0.3
do_wait
set_roty [Object 0] 0.6
do_wait
set_roty [Object 0] 0.9
do_wait
set_roty [Object 0] 1.2
do_wait
set_roty [Object 0] 1.5
do_wait
set_roty [Object 0] 1.8
do_wait
set_roty [Object 0] 2.1
do_wait
set_roty [Object 0] 2.4
do_wait
set_roty [Object 0] 2.7
do_wait
set_roty [Object 0] 3.0
do_wait
set_roty [Object 0] 3.3
do_wait
set_roty [Object 0] 3.6
do_wait
set_roty [Object 0] 3.9
do_wait
set_roty [Object 0] 4.2
do_wait
set_roty [Object 0] 4.45
do_wait
sq_actor actionlist 0 {{anim djhigh} loopstart {anim discoc} loop}
set_roty [Object 0] 4.7
set_vel [Object 0] {0.5 0 0}
do_wait time 6
+sq_object delete all
do_wait time 2
