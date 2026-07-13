start_fade 0.5 0
do_wait time 0.5
adaptive_sound changethemenow gameover
sq_audio open GameOver
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {1 0.9 0.7}
sq_pen set Cam01 [Getobjpos Info_Pos_Zwerg]
sq_pen set Hamster01 Cam01
sq_pen move Hamster01 { 6.1 0.2 -6 }
sq_pen set Hamster02 Cam01
sq_pen move Hamster02 { 6.5 0.2 -4 }
sq_pen set Hamster03 Cam01
sq_pen move Hamster03 { 6.0 0.2 -2 }
sq_pen set Cam02 Cam01
sq_pen move Cam02 { 0.9 0 0 }
sq_pen set Cam03 Cam01
sq_pen move Cam03 { 5.1 0.2 0 }
sq_actor find Stein
do_wait
sq_actor express 0 bad_sleep
do_action wait 0.2 0
action [Actor 0] wait 0
set_roty [Actor 0] 4.4
set_anim [Actor 0] mann.fallen_end_tot 10 0

sq_actor find Stein
sq_camera fix Cam01 0.715 -0.715 0.484
do_wait
sq_camera selset inout
set_anim [Actor 1] grabstein.typ_d 0 0
sq_actor find Stein
sq_actor find Dummy_Muetze_stein_a
sq_object summon Hamster Hamster01
sq_actor find Hamster
do_wait
set_sequenceactive [Actor 4] 0
set_anim [Actor 2] grabstein.typ_b 0 0
start_fade 4 1
do_wait time 1
sq_camera move Cam02 0.744 -0.39 0.11 0.1
do_wait time 8
set_anim [Actor 3] muetze_stein_aus_a.standard 0 2
do_wait time 0.2
set_anim [Actor 3] muetze_stein_a.standard 0 2
do_wait time 0.1
set_anim [Actor 3] muetze_stein_aus_a.standard 0 2
do_wait time 0.2
set_anim [Actor 3] muetze_stein_a.standard 0 2
do_wait time 0.8
set_anim [Actor 3] muetze_stein_aus_a.standard 0 2
do_wait time 0.2
set_anim [Actor 3] muetze_stein_a.standard 0 2
do_wait time 0.3
set_anim [Actor 3] muetze_stein_aus_a.standard 0 2
do_wait time 0.1
set_anim [Actor 3] muetze_stein_a.standard 0 2
do_wait time 0.3
set_anim [Actor 3] muetze_stein_aus_a.standard 0 2

do_wait time 2.8
sq_camera move Cam03 0.744 -0.32 0.17 0.1
do_wait time 6.5
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.8 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.6 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.4 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.2 {1 0.9 0.7}
do_wait time 0.1
set_anim [Actor 0] mann.fallen_end_tot 10 1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.2 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.4 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.7 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.8 {1 0.9 0.7}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 2.9 {1 0.9 0.7}
do_wait time 0.1
set_particlesource [Getobjref Feuerstelle] 0 0
set_anim [Getobjref Feuerstelle] feuerstelle.standard 0 1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.9 0.8 0.6}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.8 0.7 0.5}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.7 0.6 0.4}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.6 0.5 0.3}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.5 0.4 0.2}
do_wait time 0.1
set_anim [Actor 0] mann.fallen_end_tot 10 1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.4 0.3 0.15}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.3 0.2 0.1}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.2 0.1 0.05}
do_wait time 0.1
change_light [Getobjref Feuerstelle] {0 -0.5 0} 3 {0.3 0.1 0.0}
do_wait time 3
sq_wait 4
sq_sound 9000a 0
gametime factor 0.8
do_action walk Hamster02 4
do_action anim cleanstart 4
do_action anim cleanloop 4
do_action anim cleanstop 4
start_fade 5 0
do_action walk Hamster03 4
do_action anim beg 4
sq_wait none
sq_actor actionlist 4 { { anim cleanloop } loop }
do_action anim cleanstart 4
do_wait time 3
sq_actor actionlist 4 {}
do_wait time 2
set_hoverable [Actor 0] 0
+adaptive_sound primary menue
+cancel_fade

// Added by CHP
+sq_camera get
+set_sequence 0
+gametime stop
+gui_new_game 1





