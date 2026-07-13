sq_text file Tutorial
+adaptive_sound marker tutorial [parse_pos Music] 300
+adaptive_sound changethemenow tutorial
sq_audio open Clip_1010
+sq_actor find Zwerg

+sq_pen set Wegweiser 0
+sq_pen set Music Wegweiser
+sq_pen move Music { -30 0 0 }

+sq_pen move Wegweiser {-0.13 -0.04 1.68}
+sq_pen set P_ElfEnd Wegweiser
+sq_pen set P_Skip Wegweiser
+sq_pen move P_Skip {-2 -2 0}
+sq_pen setz P_Skip 16
sq_wait {elf}
sq_pen set Cam1 Wegweiser
sq_pen move Cam1 {-7.9 -5.55 6.32}
sq_pen set Elf1 Wegweiser
sq_pen move Elf1 {-27.4 -11.7 -3.86}
sq_pen set Elf2 Wegweiser
sq_pen move Elf2 {21 -15.2 6.32}
sq_pen set Elf3 Wegweiser
sq_pen move Elf3 {-24 -1.45 40.32}

sq_pen set Guck 0
sq_pen move Guck {-2 0 -2}
#0
sq_object summon Eisen Guck

sq_actor find Eisen
set_visibility [Object 0] 0


sq_camera fix Cam1 2.5 0.219 0.57
sq_camera selset inout
+start_fade 2 1
+option set showUI 1
do_elf path Elf1 Elf2 0.8
do_elf path Elf2 Elf3 1.0

+sq_pen set P_Crash Wegweiser
sq_pen set P_Cam P_Crash
+sq_pen set P_Zwerg P_Crash

sq_pen move P_Crash {-2 2.8 2}

sq_pen move P_Cam {-1 0 0}
+sq_pen move P_Zwerg {1 0 5}
sq_camera move P_Cam 1.14 -0.1 0 0.8
do_wait time 3.2
sq_pen move P_Cam {0 -1 0}
sq_camera move P_Cam 1.24 0.0 0 0.3

do_elf crash P_Crash
-sound play fe_schritt1 1
-sq_screenvibe kawumm
change_particlesource [Actor 0] 0 8 {-0.5 -0.5 2} {0 -0.4 0} 255 16 0 0 0 0
set_particlesource [Actor 0] 0 1
change_particlesource [Actor 0] 1 10 {-0.5 -0.5 2} {0 -0.4 0} 255 16 0 0 0 0
set_particlesource [Actor 0] 1 1
sq_actor actionlist 0 { {anim mann.angst_loop} loop}
do_action anim mann.angst_start 0
set_particlesource [Actor 0] 0 0
set_particlesource [Actor 0] 1 0
do_wait time 2
sq_actor actionlist 0  {}
do_action anim mann.angst_end 0
do_wait time 2

sq_pen set ECUPos P_Crash
sq_pen move ECUPos {0 -3 -4}
sq_camera move ECUPos 0.7 0.0 0
do_wait time 0.25
do_elf text 1100a {verzweifeln} Ach_Mist
do_wait time 1
sq_actor focus 0 1
do_action anim mann.dialog_re_negativ_c 0

do_wait time 2
sq_camera move ECUPos 1.0 -0.1 0 0.1
do_elf text 1100b {gucken_links} Nur_damit
do_wait time 2
elf movescreen 50 200
do_wait time 2
do_elf text 1100c {schmollen} Bild_dir
do_wait time 3


do_action walk P_Zwerg 0
do_wait time 2
sq_wait 0
do_wait
set_particlesource [Actor 0] 0 0
+cancel_fade
#+global viewpos P_ElfEnd; set viewpos "[lrange $P_ElfEnd 0 1] 1.5 -0.2 0.0"
sq_camera get
+global tut;if {!$tut} {start_fade 1 0}
+adaptive_sound markerenable


