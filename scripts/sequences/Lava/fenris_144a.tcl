+start_fade 0.5 0
+do_wait time 0.7
adaptive_sound changethemenow s144a
sq_audio open Fenris_144a
sq_text file Kristall
+sq_pen set Back01 0
set_roty [Actor 0] 1.57
sq_pen set TrollSource [Getobjpos Info_Pos_Zwerg]
sq_pen set Troll1 TrollSource
sq_pen move Troll1 { 0.7 -1.2 -8 }
sq_pen set Troll2 TrollSource
sq_pen move Troll2 { -1.65 -1.2 -12 }
sq_pen set Troll3 TrollSource
sq_pen move Troll3 { 0.7 -1.2 -16 }
sq_pen set Troll4 TrollSource
sq_pen move Troll4 { 2.6 -1.2 -16 }
sq_pen set Troll5 TrollSource
sq_pen move Troll5 { 4.5 -1.2 -15.5 }
do_wait
sq_color 0 {255 118 57}
sq_object summon Troll TrollSource
sq_actor find Troll 30 1
sq_object summon Troll TrollSource
sq_actor find Troll 30 1
sq_object summon Troll TrollSource
sq_actor find Troll 30 1
sq_object summon Troll TrollSource
sq_actor find Troll 30 1
sq_object summon Troll TrollSource
sq_actor find Troll 30 1
do_wait
sq_actor find Fenris_Stuhl 20 1
sq_actor find Fenris_Karte 20 1
do_wait
sq_pen set karte 7
sq_pen move karte { 0.5 0 0 }
do_action beam karte 7
set_anim [Actor 7] fenris_karte.standard 0 2
sq_pen set TrollSource2 TrollSource
sq_pen move TrollSource { 20 0 0 }
sq_pen set Cam01 0
sq_pen move Cam01 { -5 -4 0 }
sq_pen set Cam02 0
sq_pen move Cam02 { -10 -5 0 }
sq_pen set Cam03 0
sq_pen move Cam03 { -10 -5.7 0 }
sq_pen set Cam04 0
sq_pen move Cam04 { 5 -4 0 }
sq_pen set Cam05 0
sq_pen move Cam05 { 10 -3 0 }
sq_pen set Cam06 0
sq_pen move Cam06 { -12 -6.5 0 }
sq_pen set Cam07 0
sq_pen move Cam07 { 9 -4.5 0 }
sq_pen set Cam08 0
sq_pen move Cam08 { 6.7 -4.0 0 }
sq_pen set Cam09 0
sq_pen move Cam09 { 5 -3 2 }
sq_pen set P1 0
sq_pen move P1 { -6.6 0 -4.3 }
sq_camera selset inout
do_wait
sq_camera fix Cam06 1.0 0.1 -0.4
sq_wait none
+start_fade 4 1
do_wait time 6
do_action walk P1 0
do_wait time 0.5
sq_camera fix Cam02 1.8 -0.1 -0.6
sq_wait 0
do_wait
set_anim [Actor 1] 144nr1 0 0
set_anim [Actor 2] 144nr2 0 0
set_anim [Actor 3] 144nr3 0 0
set_anim [Actor 4] 144nr4 0 0
set_anim [Actor 5] 144nr5 0 0

sq_camera move Cam02 1.2 -0.1 -0.6 0.5
gametime factor 0.95
sq_wait none
do_text 0144aa 0 {  boardtalkstart boardtalkloop boardtalkloop boardtalkloop } 144aa { 50 10 }
do_wait time 5.6
gametime factor 1
sq_camera fix Cam03 0.7 -0.1 0.1
do_text 0144ab 0 { boardtalkloop boardtalkloop boardtalkloop boardtalkloop } 144ab { 50 10 }
do_wait time 4
sq_camera fix Cam02 1.1 -0.1 -0.1
sq_wait 0
do_wait
do_text 0144ac 0 { boardturnastart boardloop } 144ac { 50 10 }
sq_wait none
change_particlesource [Actor 0] 1 33 { 0 0 0 } { 0 -0.1 0 } 200 1 0 5
set_particlesource [Actor 0] 1 1
do_text 0144ad 0 { boardturnaloop boardturnaloop boardloop } 144ad { 50 10 }
do_wait time 3
sq_wait none
do_wait
sq_pen move Cam02 { 0 1 0 }
do_wait time 0.3
sq_camera fix Cam02 1.5 -0.2 0.5
sq_wait 0
change_particlesource [Actor 0] 1 33 { 0 0 0 } { 0 -0.1 0 } 200 20 0 5
do_wait
sq_pen move Cam02 { 0 -1 0 }
do_wait time 0.5
gametime factor 0.8
set_particlesource [Actor 0] 1 0
do_text 0144ae 0 { boardturnbstart boardturnbloop boardturnbloop } 144ae { 50 10 }
#------------------------------------
#------------------------------------
gametime factor 0.7
sq_wait none
do_text 0144af 0 { boardturnbloop boardturnbloop } 144af { 50 10 }
set_anim [Actor 1] 144nr1 0 1
change_particlesource [Actor 7] 1 27 { 0 0 0 } { 0 0 0 } 2048 16 0 0 0 1
set_particlesource [Actor 7] 1 1
sq_actor actionlist 7 { { anim fenris_karte.fenster_standard } loop }
do_action anim fenris_karte.zu_fenster 7
do_wait time 1
sq_camera fix Cam09 1.4 -0.1 -0.9
set_anim [Actor 2] 144nr2 0 1
set_anim [Actor 3] 144nr3 0 1
set_anim [Actor 4] 144nr4 0 1
set_anim [Actor 5] 144nr5 0 1

sq_object summon Troll_Blume TrollSource
set_visibility [Object 5] 0
sq_object summon Troll_Rettungsring TrollSource
set_visibility [Object 6] 0
sq_object summon Troll_Wecker TrollSource
set_visibility [Object 7] 0
sq_object summon Troll_Grammophon TrollSource
set_visibility [Object 8] 0
sq_object summon Troll_Klavier TrollSource
set_visibility [Object 9] 0
set_particlesource [Actor 0] 1 1
set_pos [Object 5] [parse_pos TrollSource2]
set_pos [Object 6] [parse_pos TrollSource2]
set_pos [Object 7] [parse_pos TrollSource2]
set_pos [Object 8] [parse_pos TrollSource2]
set_pos [Object 9] [parse_pos TrollSource2]
create_particlesource 6 [parse_pos Troll1] { 0 0 0} 5 0.5
do_wait time 0.5
create_particlesource 33 [parse_pos Troll1] { 0 -0.1 0} 5 0.5; set_visibility [Object 5] 1; set_visibility [Actor 1] 0
do_action beam TrollSource 1
change_particlesource [Actor 6] 1 27 { 0 0 0 } { 0 0 0 } 2048 16 0 0 0 1
set_particlesource [Actor 6] 1 1
gametime factor 0.6
do_action anim shocka 0
#do_text 0144ag 0 { shocka } 144ag { 50 10 }
set_particlesource [Actor 7] 1 0
do_wait
create_particlesource 6 [parse_pos Troll2] { 0 0 0} 20 0.5
do_wait
do_wait

set_roty [Actor 0] 1.45
create_particlesource 6 [parse_pos Troll3] { 0 0 0} 5 0.5
do_wait
do_wait
create_particlesource 33 [parse_pos Troll2] { 0 -0.1 0} 5 0.5; set_visibility [Object 6] 1; set_visibility [Actor 2] 0
do_action beam TrollSource 2

do_wait
create_particlesource 6 [parse_pos Troll4] { 0 0 0} 5 0.5
do_wait

create_particlesource 33 [parse_pos Troll3] { 0 -0.1 0} 5 0.5; set_visibility [Object 7] 1; set_visibility [Actor 3] 0
do_action beam TrollSource 3

create_particlesource 6 [parse_pos Troll5] { 0 0 0} 20 0.5
do_wait

do_wait

create_particlesource 33 [parse_pos Troll4] { 0 -0.15 0} 5 0.5; set_visibility [Object 8] 1; set_visibility [Actor 4] 0
do_action beam TrollSource 4

do_wait

create_particlesource 33 [parse_pos Troll5] { 0 -0.2 0} 5 0.5; set_visibility [Object 9] 1; set_visibility [Actor 5] 0
do_action beam TrollSource 5

sq_actor actionlist 6 { { anim fenris_stuhl.fernseher} loop }
do_action anim fenris_stuhl.zu_fernseher 6

sq_wait none
sq_camera fix Cam04 2.2 -0.1 -0.2
gametime factor 0.9
do_text 0144ah 0 { shockb } 144ah { 50 10 }
do_wait time 2
set_particlesource [Actor 6] 1 0
do_wait time 0.8
-sq_screenvibe steps
sq_wait 0
do_wait
sq_camera fix Cam05 1.5 -0.3 0.3
do_text 0144aj 0 { tvtalkaloop tvtalkaloop } 144aj { 50 10 }
sq_camera fix Cam05 2.0 -0.1 -0.5
sq_wait none
do_text 0144ak 0 { tvtalkaloop tvtalkaloop tvtalkatob tvtalkbstart tvtalkbloop } 144ak { 50 10 }
do_wait time 3
sq_camera fix Cam07 1.0 -0.1 -0.4
sq_wait 0
do_wait
#do_text 0144al 0 { tvtalkatob  tvtalkbstart tvtalkbloop } 144al { 50 10 }
sq_camera move Cam08 1.1  -0.1 -0.7
#------------------------------------
# Schaum kommt
#------------------------------------
do_action anim tvtalkbstop 0
#do_text 0144am 0 { tvtalkbstop } 144m { 50 10 }
do_text 0144am 0 { lolli } 144am { 50 10 }
+set_particlesource [Actor 0] 1 0
start_fade 3 0
do_wait time 4
+do_action beam Back01 0
+cancel_fade
+adaptive_sound changetheme kristall
+sq_object delete all
+do_wait


