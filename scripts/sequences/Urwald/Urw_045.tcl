sq_audio open 0045
sq_text file Urwald

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s045
#-----------------------------------------


sq_pen set RiechPos TriggerPos
sq_pen move RiechPos {-1 0 0}

sq_pen set WalkInPos RiechPos
sq_pen move WalkInPos {-2 0 -1}

sq_pen set WalkAwayPos RiechPos
sq_pen move WalkAwayPos {-5.5 0 -6}

sq_pen set GoMadPos WalkAwayPos
sq_pen move GoMadPos {8 0 -4}

sq_wait camera
sq_wait all

sq_camera move RiechPos 1.1 -0.3 0.4
do_action walk WalkInPos 0
sq_camera fix 0 0.7 0.0 -0.2
do_wait itme 2.5
do_action anim scratchhead 0

sq_actor eyes 0 {c c 9 c c c c 9 c c}
do_wait time 2
sq_camera fix RiechPos 1.1 -0.3 0.4
do_action walk RiechPos 0
do_wait time 1
sq_camera fix RiechPos 1.0 -0.3 -0.5
do_action anim sniffatfood 0
do_wait time 1
do_action anim scratchhead 0

do_action anim wipenose 0
do_action anim dontknow 0
sq_wait none
sq_camera fix RiechPos 0.9 -0.3 0.6
do_action rotate WalkAwayPos 0
do_action run WalkAwayPos 0
do_wait time 1
sq_camera fix 0 0.8 -0.2 -0.8
do_wait time 2
sq_wait all
do_action anim washface 0
do_wait time 1

do_action rotate 1.1 0
sq_camera fix 0 0.65 0.0 1.0
sq_actor eyes 0 {c c 9 c c c c 9 c c}
do_wait time 1
sq_actor mouth 0 {2}
sq_wait none
sq_actor eyes 0 {c c 9 c c c c 9 c c 9 c c}
do_wait time 1
sq_wait all
do_action anim sleepwalk 0
sq_sound Wuerg
do_wait time 1
do_wait time 0.5
sq_actor mouth 0 {2}
sq_actor eyes 0 { l l r r  o o u u  u u  o o l l r r c c o o}
#do_wait time 1
do_action anim sleeptosit 0
do_wait time 2
do_action anim standup 0
do_wait time 0.5
sq_actor eyes 0 {10}
do_text 045b 0 Auto Was_fuern
do_wait camera
sq_camera move 0 1.2 -0.4 0.4
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker metall [get_pos this]
adaptive_sound changethemenow metall
#-----------------------------------------

