#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmourwald
#-----------------------------------------
sq_audio open Urw_025b
sq_text file Urwald
sq_actor find Zwerg 15 1 0
do_wait time 0.1
sq_color 0 {Voodoo1}
sq_color 1 {Wiggle1}
sq_pen set P1 0
sq_pen set P2 1
sq_pen set P4 0
sq_pen move P1 {0 0 4}
sq_pen set P3 P1
sq_pen move P4 {1.25 0 0}
sq_pen move P2 {0.5 0 1}
sq_actor focus 0 1
sq_actor focus 1 0
sq_camera move 0 0.9 -0.0 -0.0 3
sq_wait none
sq_actor actionlist 1 {{rotate P1} {anim standloopa} {anim standloopa} {anim tocomfort} loopstart {{anim standloopa} {anim standloopb} {anim standloopc} {anim standloopd} {anim wait} {anim teeter_w}} loop}
do_action walk P2 1
do_wait time 1
sq_wait 0
sq_actor express 0 good_normal
do_action walktired P1 0
do_action rotate 1 0
do_action anim getcomfort 0
do_text 025a 0 Auto 025a
sq_camera move P3 1.1 -0.05 -0.3
do_text 025b 0 Auto 025b
sq_pen move P3 {-0.5 0 0}
sq_pen move P2 {-0.5 0 -0.5}
sq_camera fix P3 0.8 -0.05 0.8
sq_actor actionlist 1 {{rotate 0} loopstart {{anim standloopa} {anim standloopb} {anim standloopc} {anim standloopd} {anim wait} {anim teeter_w}} loop}
do_action walk P2 1
do_action rotate left 0
do_text 025c 0 Auto 025c
sq_camera fix 0 0.7 0.0 -0.9
sq_actor focus 0 none
sq_actor eyes 0 {o o o o o o o o o o}
do_action rotate right 0
sq_actor eyes 0 {o o o 9 9 9 9 9 9 9 9 9 9 9 9 9 o 9 r r r r 9 9 9 c }
do_text 025d 0 {protecteyesstart protecteyesloop protecteyesloop protecteyesstop} 025d
sq_camera fix P3 0.9 -0.05 0.9
sq_actor eyes 0 {c c }
do_action rotate 1 0
do_text 025e 0 {showright} 025e
sq_camera fix P3 0.9 -0.05 -0.9
sq_actor actionlist 1 {}
sq_actor actionlist 0 {{{anim standloopa} {anim standloopb} {anim standloopc} {anim standloopd} {anim wait} {anim teeter_w}} loop}
sq_wait 1
sq_actor focus 0 1
do_text 025fb 1 Auto 025f
sq_actor actionlist 0 {}
sq_actor actionlist 1 {{{anim standloopa} {anim standloopb} {anim standloopc} {anim standloopd} {anim wait} {anim teeter_w}} loop}
sq_wait 0
sq_camera fix P3 0.8 -0.05 -0.9
do_text 025gb 0 Auto 025g
sq_wait none
do_text 025hb 0 {scratchhead showleft} 025h
do_wait time 2
sq_camera move P4 0.7 -0.1 -0.3 3
do_wait time 4
sq_camera fix P3 1 -0.2 0.0
sq_pen move P1 {5 0 0}
sq_pen move P2 {1 0 0}
sq_wait 0
do_text 025j 0 Auto 025j
sq_actor actionlist 1 {}
sq_wait 1
do_action rotate P1 1
do_action run P1 0
do_wait time 1
sq_wait none
do_action walk P2 1
do_wait time 0.8
sq_camera fix P3 0.8 -0.2 -0.6
sq_wait 1
do_wait time 1.5
do_action rotate right 1
sq_actor mouth 1 {c 4 4 4 4 c}
do_wait time 0.5
sq_actor mouth 1 {c 6 6 6 6 6 6 6 6 6 6 c}
sq_actor eyes 1 {c 12 12 12 12 12 12 12 12 12 12 c}
do_action anim wipenose 1
sq_camera move P3 1.0 -0.2 -0.6
do_wait time 0.2
sq_actor focus 0 none
sq_actor focus 1 none
+del [ref_get [Actor 0] muetze2]
+call_method [Actor 0] destroy
do_text 025o 1 {scratchhead} 025o
do_text 025p 1 {insane} 025p
sq_camera get
do_wait
