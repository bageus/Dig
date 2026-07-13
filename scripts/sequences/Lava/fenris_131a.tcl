start_fade 0.5 0
do_wait time 0.5
sq_audio open Fenris_131a
sq_text file Schwefel
adaptive_sound changethemenow s131a
sq_pen set zurueck 0
sq_pen set Cam01 0
sq_pen move Cam01 { 0 -3.5 0 }
sq_pen set Text01 0
sq_pen move Text01 { 0 5 14 }
sq_pen set Cam02 0
sq_pen move Cam02 { -3 0 0 }
sq_pen set Cam03 0
sq_pen move Cam03 { -7 -1 3 }
sq_pen set Cam04 0
sq_pen move Cam04 { 8 -0.5 6 }
sq_pen set Cam05 0
sq_pen move Cam05 { 3 -0.5 6 }
sq_pen set Cam06 0
sq_pen move Cam06 { 1 -0.5 6 }
sq_pen set Cam07 0
sq_pen move Cam07 { 0 -3 0 }
sq_pen set Troll01 0
sq_pen move Troll01 { -3.65 0.36 0.3 }
sq_pen set Troll02 0
sq_pen move Troll02 { -7 0.36 0.3 }
sq_pen set Troll03 0
sq_pen move Troll03 { -9 0.36 4 }
sq_pen set Troll04 0
sq_pen move Troll04 { 1 0.25 8 }
sq_pen set Troll05 0
sq_pen move Troll05 { 8 0.36 13 }
sq_pen set Troll06 0
sq_pen move Troll06 { 3 0.36 13 }
sq_pen set Troll07 0
sq_pen move Troll07 { 0 0.5 5 }
sq_color 0 {255 118 57}

sq_pen set oben [Getobjpos Info_Pos_Zwerg]
sq_object summon Stein
sq_object summon Troll Troll01
do_wait
sq_actor find Troll
set_roty [Object 1] 0.8
sq_object summon Troll Troll02
sq_actor find Troll
sq_object summon Troll Troll03
sq_actor find Troll
sq_object summon Troll Troll04
sq_actor find Troll
sq_object summon Troll Troll07
sq_actor find Troll
sq_object summon Troll Troll05
sq_actor find Troll
do_wait
sq_object summon Zwerg Text01
set_visibility [Object 7] 0
sq_actor find Zwerg
do_action rotate 2 3
do_action rotate 3 2
do_action rotate front 4
set_roty [Actor 5] 0.6

change_particlesource [Object 0] 1 7 { 0 0 0 } { 0 0 0 } 400 1 0
set_particlesource [Object 0] 1 1
do_wait
do_action anim fenrir.thron_sitzen 0
set_physic [Object 0] 0
set_visibility [Object 0] 0
set_pos [Object 0] [parse_pos oben]
sq_pen move oben { 0 1 0 }
do_wait
sq_camera fix oben 1.55 0.4 -0.1
start_fade 3 1
do_wait time 2
change_particlesource [Object 0] 1 7 { 0 0 0 } { 0 0 0 } 400 2 0
do_wait time 2
sq_pen move oben { 0 5 0 }
change_particlesource [Object 0] 2 22 { 0 0 0 } { 0 0 0 } 400 5 0 0 4
set_particlesource [Object 0] 2 1
change_particlesource [Object 0] 1 7 { 0 0 0 } { 0 0 0 } 400 3 0
do_wait time 1
change_particlesource [Object 0] 1 22 { 0 0 0 } { 0 0 0 } 400 10 0 0 4
change_particlesource [Object 0] 3 22 { 0 4 0 } { 0 0 0 } 400 10 0 0 4
change_particlesource [Object 0] 4 22 { 0 8 0 } { 0 0 0 } 400 10 0 0 4
set_particlesource [Object 0] 3 1
set_particlesource [Object 0] 4 1
do_wait time 0.5
sq_camera selset inout
sq_camera move Cam01 0.8 -0.2 0.1 0.2
do_wait time 1
do_text " " 7 NoAnim 131ad
do_wait time 1
+set_particlesource [Object 0] 1 0
+set_particlesource [Object 0] 2 0
do_wait time 2
do_text 0131aa 0 {fenrir.thron_verzweifeln} 131aa { 30 20 }
do_wait time 1
+set_particlesource [Object 0] 3 0
sq_wait 0
do_wait
sq_pen set spritz1 2
sq_pen move spritz1 { -0.4 -0.5 0.7 }
sq_pen set spritz2 3
sq_pen move spritz2 { 0.4 -0.5 0.7 }
sq_camera fix Cam02 0.8 -0.2 -0.1
do_action anim 131jump 1
sq_camera move Cam03 0.8 -0.3 -0.4 0.3
sq_actor actionlist 2 { { anim hit } { anim 131funbath } loop }
sq_actor actionlist 3 { { anim 131funbath } { anim hit } loop }
do_action anim 131funbath 2
do_wait time 0.3
do_action anim hit 3
do_wait time 0.3
do_particle create 21 spritz2 { 0.1 -0.05 -0.2 } 10 0.2
do_particle create 21 spritz1 { -0.1 -0.05 0.2 } 10 0.2
do_wait time 0.5
do_particle create 21 spritz1 { -0.1 -0.05 0.2 } 10 0.2
do_wait time 0.5
do_particle create 21 spritz1 { -0.1 -0.05 0.2 } 10 0.2
do_wait time 0.6
do_particle create 21 spritz1 { -0.1 -0.05 0.2 } 10 0.2
do_particle create 21 spritz2 { 0.1 -0.05 -0.2 } 10 0.2
do_wait time 0.5
do_particle create 21 spritz2 { 0.1 -0.05 -0.2 } 10 0.2
do_wait time 0.5
do_particle create 21 spritz2 { 0.1 -0.05 -0.2 } 10 0.2
do_wait time 1
set_anim [Actor 4] 131castaway 0 2
sq_camera move Cam06 0.8 0.05 0.1 0.3
do_wait time 2
do_action anim 131dive 5
do_wait time 3
sq_camera move Cam04 1 0.05 0.4 0.2
do_wait time 1
do_action walktired Troll06 6
do_wait time 3.5
sq_camera move Cam07 1.2 0.25 -0.3 2
sq_wait 6
do_wait camera
sq_wait none
sq_actor actionlist 6 { { anim standstill } { anim 131reachstart } loopstart { anim 131reachloop } loop }
do_action rotate 0 6
do_text 0131ab 0 { thronnanana } 131ab { 30 10 }
do_wait time 2.5
do_text " " 7 NoAnim 131ac
do_wait time 1.5
start_fade 3 0
do_wait time 3
+set_particlesource [Object 0] 4 0
+sq_object delete all
+do_wait
+cancel_fade
adaptive_sound changethemenow titanic


