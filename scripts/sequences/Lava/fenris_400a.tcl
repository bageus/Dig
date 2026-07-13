start_fade 1 0
do_wait time 1
#----->einkommentieren zum testen
sq_pen set fallpos [Getobjpos Fenris_Stuhl]
sq_pen set Zwerg001 [Getobjpos Fenris_Stuhl]
sq_pen move Zwerg001 { -4 0 4 }
sq_pen move fallpos { -14 0 0 }
sq_pen setz fallpos 6
sq_pen set wech fallpos
sq_pen move wech { 50 0 7 }

do_wait

+sq_object summon Fenris_400 fallpos
sq_actor find Fenris_400
sq_pen set Fenris fallpos
sq_pen set Zwerg fallpos
sq_pen set Cam fallpos
sq_pen set Cam2 fallpos
sq_pen set Fifi fallpos
sq_pen set fenriszurueck 0
sq_pen move Fenris { 0 -4 0 }
sq_pen move Fifi { -2 0 0 }
sq_pen move Zwerg { 0 0 8 }
sq_pen move Cam { 0 -2 8 }
sq_pen move Cam2 { 0 -0.2 8 }
do_wait
do_action beam wech 0
+sq_object summon Zwerg TriggerPos 5
do_wait
set_visibility [Object 1] 0
sq_actor find Zwerg 200 1 0
+sq_object summon Dummy_Muetze_kampf_02_a
link_obj [Object 2] [Actor 2] 4
+sq_object summon Schwert_1
link_obj [Object 3] [Actor 2] 0
sq_wait none
sq_camera fix Cam 0.8 0.3 -0.1
sq_actor actionlist 2 { { anim cheer } loop }
start_fade 1 1
do_action beam Zwerg 2
do_action rotate front 2
gametime factor 0.5
do_action anim knee_shake 1
sq_camera selset inout
sq_wait 1
do_wait
do_action anim cheer 2
#gametime factor 1
sq_camera move Cam2 0.8 0.4 -0.1 3
sq_pen move Zwerg { 10 0 -10 }
sq_wait none
sq_actor eyes 2 { c l l 2 2 2 2 2 2 2 2 2 2 8 7 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 c }
sq_actor mouth 2 { c c c 15 15 14 14 11 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 c }
do_action anim knee_roar 1
#gametime factor 0.2
do_action wait 0.1 2
sq_actor actionlist 2 {}
do_wait time 0.5
sq_actor actionlist 1 {}
sq_wait 1
do_wait
sq_wait none
do_action anim knee_fall 1
do_wait time 0.3
-sound play equake4 1
-sq_screenvibe equake4
sq_wait 1
do_wait
+gametime factor 1
start_fade 2 0
do_wait time 2
sq_pen move Fenris { 0 4 0 }
sq_pen set Cam01 Fenris
sq_pen setz Cam01 13
sq_pen set Cam02 Cam01
sq_pen move Cam02 { 0 -4 0 }
sq_object delete 3
sq_pen set Zwerg1 Fenris
sq_pen move Zwerg1 { -3.5 0 7 }
sq_pen set Zwerg2 Fenris
sq_pen move Zwerg2 { -1.5 0 7 }
sq_pen set Zwerg3 Fenris
sq_pen move Zwerg3 { -2.5 0 7 }
sq_pen set Zwerg4 Fenris
sq_pen move Zwerg4 { -1 0 7 }
sq_pen set light1 Fenris
sq_pen move light1 { -1.5 -3.5 0 }
+sq_object summon Dummy_Muetze_metall_a wech
+sq_object summon Fifi_mit_Gleipnir wech
sq_actor find Fifi_mit_Gleipnir
sq_camera fix Cam01 1.0 -0.3 0.7
sq_wait none
do_wait time 2
start_fade 3 1
do_wait time 0.5
set_visibility [Actor 3] 0
set_roty [Actor 2] 1.57
do_action anim standup 2
set_pos [Actor 2] [parse_pos Zwerg4]
sq_wait 2
do_wait
do_action drunk Zwerg3 2
gametime factor 1

change_particlesource [Actor 1] 3 11 { -2.25 -0.5 0.25 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 1 11 { -1.75 -1 0.25 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 2 11 { -1 -1.25 0.25 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 4 11 { -0.25 -1 0.25 } { 0 0 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 5 13 { -2.25 -0.5 0.25 } { 0 -0.1 0 } 50 3 0 10 0.5
change_particlesource [Actor 1] 6 13 { -2.25 -0.5 0.25 } { -0.02 -0.05 0 } 50 3 0 10 0.5
do_action rotate right 2
do_action anim breathe 2
do_action anim hatofhead 2
link_obj [Object 2] [Actor 2] 0
do_action anim hatofhand 2
link_obj [Object 2]
set_pos [Object 2] [parse_pos wech]
do_action anim hatofgone 2

do_action anim hatongone 2
link_obj [Object 3] [Actor 2] 0
do_action anim hatonhand 2
link_obj [Object 3] [Actor 2] 4
do_action anim hatonhead 2

do_action walkfit Zwerg2 2
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
sq_wait none
do_action anim walkback 2
+sq_object summon Dummy_L1 wech
do_wait
set_visibility [Object 5] 0; set_anim [Object 5] l1.null 0 2
link_obj [Object 5] [Actor 1] 2
do_action beam Fifi 3
do_wait time 0.6
gametime factor 0.7
sq_camera move Cam02 2.0 0.1 0.2 0.3
sq_wait none
-sound play equake7
-sq_screenvibe equake7
do_action anim fenris2fifi 1
do_wait time 2.5
set_visibility [Object 5] 1; set_anim [Object 5] l1.standard 0 1
sq_wait 1
do_wait
sq_wait none
link_obj [Object 5]
set_visibility [Actor 1] 0; set_visibility [Actor 3] 1
do_action anim newfifi 3
+gametime factor 1
sq_camera move Fifi 1.3 -0.2 0.2 0.4
do_wait time 2
set_visibility [Object 5] 0
do_action beam wech 1
do_wait time 1
#+do_action beam fenriszurueck 0
#+sq_object delete all
+sq_object delete all
+del [Actor 0]
set_pos [Actor 2] [parse_pos Zwerg001]
+foreach t [obj_query [get_ref this] -class Troll -range 100] {del $t}
+foreach t [obj_query [get_ref this] -class Trollschild_3 -range 100] {del $t}
+foreach t [obj_query [get_ref this] -class Krumsaebel -range 100] {del $t}
+foreach t [obj_query [get_ref this] -class Zauberstab -range 100] {del $t}
+foreach t [obj_query [get_ref this] -class Hellebarde -range 100] {del $t}
+cancel_fade
+do_wait


