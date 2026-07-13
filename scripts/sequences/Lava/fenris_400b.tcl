sq_text file Lava
sq_audio open Fenris_400b
start_fade 1 0
sq_pen set Fenris [Getobjpos Fenris_Stuhl]
sq_pen set Zwerg001 [Getobjpos Fenris_Stuhl]
sq_pen move Zwerg001 { -4 0 4 }
sq_pen set wech Fenris
sq_pen move wech { 50 0 0 }
do_wait time 1
sq_pen set Fenriszurueck [Getobjpos Fenris_Stuhl]
do_action beam wech 0
+sq_object summon Fenris_Drunk Fenris
+sq_actor find Fenris_Drunk
do_wait time 0.2
sq_pen move Fenris { 0 -3.5 0 }
sq_camera fix Fenris 1.2 0.2 0.3
set_roty [Actor 1] 1.57
sq_camera addset speed 0 {{ s 0.1 1.0} {s 0.5 0.6} {s 0.9 1.0} {s 1.0 0.0} }
sq_pen set Cam01 Fenriszurueck
sq_pen move Cam01 { -4.5 -2 0 }
sq_pen set Cam02 Fenriszurueck
sq_pen move Cam02 { 1 -4 0 }
sq_pen set Cam03 Fenriszurueck
sq_pen move Cam03 { 11 -1 0 }
sq_pen set Fen01 Fenriszurueck
sq_pen move Fen01 { 3 0 10 }
sq_pen set Fen02 Fen01
sq_pen move Fen02 { 10 0.2 -7 }
sq_pen set fump Fen01
sq_pen move fump { 10 -4 0 }

sq_pen set Cam04 Cam02
sq_pen move Cam04 { 10 0 -7 }
sq_actor actionlist 1 { { anim sitloop } loop }
do_action anim sitloop 1
start_fade 3 1
do_wait time 2
sq_color 1 {255 118 57}
adaptive_sound changethemenow s400a
sq_actor actionlist 1 {}
sq_wait 1
do_action anim drinkstarta 1
sq_actor actionlist 1 { { anim drinkloop } { anim drinkloop } { anim drinkstopb } }
do_action anim drinkstartb 1
-sq_screenvibe steps
sq_camera fix Cam01 0.7 -0.1 -0.4
do_action anim drinkstopa 1
sq_camera fix Fenris 1.9 0.2 0.3
+gametime factor 1
do_text 400ba 1 { sittalkb sittalkc sittalkb } 400ba
do_text 400bb 1 { sittalkb sittalkc getupfast } 400bb { 40 20 }
start_fade 0.1 0
set_pos [Actor 1] [parse_pos Fen01]
screenvibe 0.7 5 0.2 0.5 3 0.5 5
set_roty [Actor 1] 4.71
sq_camera selset speed
do_wait
sq_camera fix Cam02 1.1 -0.2 -0.5
start_fade 0.1 1
sq_wait none
do_text 400bd 1 { drunkstand } 400bd { 40 20 }
+sq_object move 0 Fen02 0.3
sq_camera move Cam04 0.8 -0.2 -0.5 0.2
do_wait time 4
do_action anim drunkfall 1
do_wait time 2
sq_camera move Cam03 1.7 -0.2 -0.5
do_wait time 2
start_fade 2 0
do_wait time 2
sq_pen set WigglesPos Fenriszurueck
sq_pen move WigglesPos { 22 0 0 }
sq_pen setz WigglesPos 14
sq_pen set Wiggles2Pos Fenriszurueck
sq_pen move Wiggles2Pos { 17 0 6.5 }
sq_pen set Wiggles3Pos Fenriszurueck
sq_pen move Wiggles3Pos { 18 0 7 }
sq_pen set Cam06 Fenriszurueck
sq_pen move Cam06 { 15.5 -1 3 }
sq_pen set Cam07 Fenriszurueck
sq_pen move Cam07 { 15.5 -5 3 }
do_wait time 0.5
+sq_object summon Zwerg TriggerPos 5
+sq_actor find Zwerg 200 1 0
do_action beam WigglesPos 2
set_visibility [Object 1] 0
sq_camera fix Wiggles2Pos 1.0 -0.1 -0.7

do_action rotate back 2
do_wait time 2
start_fade 3 1
do_wait time 2.5
sq_wait 2
gametime factor 1
change_particlesource [Actor 1] 3 11 { -4.5 -0.25 -0.5 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 1 11 { -4 -0.75 -0.5 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 2 11 { -3 -1.25 -0.5 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 4 11 { -2 -0.75 -0.5 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 5 13 { -4.5 -0.25 -0.5 } { 0 -0.1 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 6 13 { -4.5 -0.25 -0.5 } { -0.02 -0.05 0 } 50 3 0 10 0.5
+sq_object summon Dummy_Muetze_metall_a
do_action walkfit Wiggles3Pos 2
do_action anim breathe 2
do_action anim hatongone 2
link_obj [Object 2] [Actor 2] 0
do_action anim hatonhand 2
link_obj [Object 2] [Actor 2] 4
do_action anim hatonhead 2

do_action walkfit Wiggles2Pos 2
do_action rotate 2.5 2
gametime factor 2
sq_wait none
sq_actor actionlist 2 { { anim worktopmetall } { anim worktopmetall } { anim hammerloopmetall } { anim hammerloopmetall } { anim hammerloopmetall } { anim worktopmetall } { anim worktopmetall } }
do_action anim worktopmetall 2
set_particlesource [Actor 1] 1 1
do_wait time 0.2
set_particlesource [Actor 1] 2 1
do_wait time 0.2
set_particlesource [Actor 1] 3 1
do_wait time 0.2
set_particlesource [Actor 1] 4 1
do_wait time 0.2
set_particlesource [Actor 1] 5 1
do_wait time 0.2
set_particlesource [Actor 1] 6 1
sq_wait 2
do_wait
set_particlesource [Actor 1] 1 0
set_particlesource [Actor 1] 2 0
set_particlesource [Actor 1] 3 0
set_particlesource [Actor 1] 4 0
set_particlesource [Actor 1] 5 0
set_particlesource [Actor 1] 6 0
gametime factor 1
do_action anim tired 2
sq_wait none
gametime factor 2
sq_actor actionlist 2 { { anim worktopmetall } { anim worktopmetall } { anim hammerloopmetall } { anim hammerloopmetall } { anim hammerloopmetall } { anim worktopmetall } { anim worktopmetall } }
do_action anim worktopmetall 2
set_particlesource [Actor 1] 1 1
do_wait time 0.2
set_particlesource [Actor 1] 2 1
do_wait time 0.2
set_particlesource [Actor 1] 3 1
do_wait time 0.2
set_particlesource [Actor 1] 4 1
do_wait time 0.2
set_particlesource [Actor 1] 5 1
do_wait time 0.2
set_particlesource [Actor 1] 6 1
sq_wait 2
do_wait
+set_particlesource [Actor 1] 1 0
+set_particlesource [Actor 1] 2 0
+set_particlesource [Actor 1] 3 0
+set_particlesource [Actor 1] 4 0
+set_particlesource [Actor 1] 5 0
+set_particlesource [Actor 1] 6 0
+gametime factor 1
do_action rotate 1.7 2
sq_wait none
+sq_object summon Fifi_mit_Gleipnir wech
+sq_actor find Fifi_mit_Gleipnir
+sq_object summon Dummy_L1
set_anim [Object 4] l1.null 0 2; set_visibility [Object 4] 0; set_pos [Object 4] [parse_pos fump];
do_wait
set_visibility [Actor 3] 0
do_action anim walkback 2
sq_camera move Cam07 2.0 0.1 0.2 0.3
set_pos [Actor 3] [parse_pos Fen02];
sq_wait none
gametime factor 0.7
do_action anim fenris2fifi 1
do_wait time 2
set_visibility [Object 4] 1; set_anim [Object 4] l1.standard 0 1
-sound play equake7 1
-sq_screenvibe equake7
sq_wait 1
do_wait
set_visibility [Actor 1] 0; set_visibility [Actor 3] 1
sq_wait none
+gametime factor 1
do_action anim newfifi 3
sq_camera move Fen02 1.3 -0.2 0.2 0.4
do_wait time 3
set_visibility [Object 4] 0
set_pos [Actor 1] [parse_pos wech]
+sq_object delete all
+del [Actor 0]
set_pos [Actor 2] [parse_pos Zwerg001]
+foreach t [lnand 0 [obj_query [get_ref this] -class Trollschild_3 -range 100]] {del $t}
+foreach t [lnand 0 [obj_query [get_ref this] -class Krumsaebel -range 100]] {del $t}
+foreach t [lnand 0 [obj_query [get_ref this] -class Zauberstab -range 100]] {del $t}
+foreach t [lnand 0 [obj_query [get_ref this] -class Hellebarde -range 100]] {del $t}
+foreach t [lnand 0 [obj_query [get_ref this] -class Troll -range 100]] {del $t}
#+do_action beam Fenriszurueck 0
+cancel_fade
+do_wait








