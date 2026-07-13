#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmourwald
#-----------------------------------------
sq_wait none
sq_actor actionlist 0 { { anim climbstillani } loop }
do_action anim climbstillani 0
sq_pen set kfahrt_punkt1 TriggerPos
sq_pen move kfahrt_punkt1 {3 0 0}
sq_camera selset inout
sq_camera move kfahrt_punkt1 1 0 -0.2 0.2
do_wait camera
do_wait time 2
+set tuer [obj_query 0 -class Tuer_kaserne -owner 6 -limit 1]; call_method $tuer oeffnen [get_ref this] -1
do_wait time 5
sq_wait none
do_wait
