#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmourwald
#-----------------------------------------
sq_text file Urwald
sq_audio open Wasserraetsel
sq_wait 0
#sq_actor find Schatztonne 40 1
sq_pen set kfahrt_punkt1 TriggerPos
sq_pen move kfahrt_punkt1 {5 10 -5.6}
sq_pen set kfahrt_punkt2 kfahrt_punkt1
sq_pen move kfahrt_punkt2 { -3 -6 0 }
sq_pen set walkp TriggerPos
sq_pen move walkp { 0.7 0 0 }
sq_camera selset inout
sq_camera move 0 1.2 -0.2 -0.3
do_action walk TriggerPos 0
sq_camera addset inspeed 0.0 {{ s 1.0 1.0}}
sq_camera addset outspeed 1.0 {{ s 1.0 0.0}}
do_action rotate left 0
sq_wait none
sq_camera selset inspeed
sq_wait none
do_text 030a 0 { lookdownstart lookdownloop lookdownloop lookdownloop lookdownloop lookdownloop} 30a
do_wait time 1
sq_camera move kfahrt_punkt2 1.3 -0.2 0.3 0.22
do_wait time 4.2
sq_camera selset outspeed
do_action walk walkp 0
do_action rotate front 0
sq_camera move kfahrt_punkt1 1.2 -0.4 0.5 0.22
do_wait camera
do_wait time 3
#sq_camera get
sq_wait all
do_wait

