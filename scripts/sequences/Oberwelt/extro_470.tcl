#Clip 470 - Riesenelfe stirbt

#Wenn die Elfe getötet wird, erschafft sie den Trigger, der die Sequenz auslöst, an der Stelle,
#wo der Absturz stattfindet. Dort soll dann eine Sequenzelfe gesummont werden (Abblende), die mit
#gestutzten Flügeln ihren Absturz vollführt.

#alle Methoden usw. in characters\riesenelfe.tcl
adaptive_sound changetheme s470


sq_camera selset inout
sq_wait none

sq_pen set toteelfe TriggerPos
sq_pen move toteelfe {0 -4 0}
sq_pen set ungefaehrAufschlag toteelfe
sq_pen move ungefaehrAufschlag {0 9 0}



gametime factor 0.1
start_fade 0.2 0
gametime factor 1
sq_object summon Riesenelfe_Sequenz TriggerPos
sq_actor find Riesenelfe_Sequenz 100 1 any
+call_method [Actor 0] fluegelab
sq_camera fix toteelfe 1.5 0.5 0
start_fade 1 1

#1
sq_pen set AusloeserPos ungefaehrAufschlag
sq_pen move  {1 0 0 }
sq_object summon Stein AusloeserPos
set_visibility [Object 1] 0
#2
sq_pen set Ausloeser2Pos ungefaehrAufschlag
sq_pen move  {-1 0 0 }
sq_object summon Stein Ausloeser2Pos
set_visibility [Object 2] 0
#3
sq_pen set Ausloeser2Pos ungefaehrAufschlag
sq_pen move  {2.5 -0.5 0 }
sq_object summon Stein Ausloeser2Pos
set_visibility [Object 3] 0


change_particlesource [Actor 0] 0 8 {2 -2 0} {0 0 0} 255 4 0 0 0 0
set_particlesource [Actor 0] 0 0
change_particlesource [Actor 0] 1 33 {0 0 0} {0 -0.075 0} 155 4 0 0 0 1
set_particlesource [Actor 0] 1 1
change_particlesource [Actor 0] 2 34 {0 0 0} {0 0.0 0} 155 4 0 0 0 1
set_particlesource [Actor 0] 2 1
#Magiiiiiieee
change_particlesource [Actor 0] 3 34 {0 0 0} {-0.4 -0.4 0} 255 4 0 0 0 0
set_particlesource [Actor 0] 3 0
change_particlesource [Actor 0] 4 34 {0 0 0} {0.4 -0.4 0} 255 4 0 0 0 0
set_particlesource [Actor 0] 4 0
change_particlesource [Actor 0] 5 34 {0 0 0} {0.4 -0.1 0.7} 255 4 0 0 0 0
set_particlesource [Actor 0] 5 0
change_particlesource [Actor 0] 6 34 {0 0 0} {-0.4 0 1.4 } 255 4 0 0 0 0
set_particlesource [Actor 0] 6 0


sq_pen set Blitz1 ungefaehrAufschlag
sq_pen move Blitz1 {0 4 0}
sq_pen set Blitz2 ungefaehrAufschlag
sq_pen move Blitz2 {-10 -7 0}

sq_pen set Blitz3 ungefaehrAufschlag
sq_pen move Blitz3 {2.5 4 0}
sq_pen set Blitz4 ungefaehrAufschlag
sq_pen move Blitz4 {5 -7 3}

sq_pen set Blitz3 ungefaehrAufschlag
sq_pen move Blitz3 {2.5 4 0}
sq_pen set Blitz4 ungefaehrAufschlag
sq_pen move Blitz4 {7 -7 3}

sq_pen set Blitz6 ungefaehrAufschlag
sq_pen move Blitz6 {1.5 4 2}
sq_pen set Blitz5 ungefaehrAufschlag
sq_pen move Blitz5 {0 -7 6}

do_wait time 1
+call_method [Actor 0] absturz
do_wait time 1.8
sound play riesenelfe_tod 1
sq_pen move ungefaehrAufschlag {0 2 0}
+sq_camera move ungefaehrAufschlag 1.7 -0.3 0 0.5
do_wait time 1.7
sound play equake4 1
do_wait time 1.0
set_particlesource [Actor 0] 0 1
sq_screenvibe equake4
sound play fe_schritt2 1
do_wait time 0.5
sound play fe_schritt1 1
set_particlesource [Actor 0] 0 0
do_wait time 0.5
sound play fe_schritt2 1
sq_screenvibe kawumm
+sq_camera move ungefaehrAufschlag 1.7 -0.3 0 0.38
do_wait time 3
+sq_camera move ungefaehrAufschlag 1.9 -0.3 0 0.1
sound play magic_b 1
sq_screenvibe kawumm
lightning [parse_pos Blitz2] [parse_pos Blitz1] {0.1 0 0} 12.0 0.011765 0.913725 0.913725
lightning [parse_pos Blitz2] [parse_pos Blitz1] {0 0 0} 12.0 0.760784 0.760784 0.760784
lightning [parse_pos Blitz2] [parse_pos Blitz1] {0 0 0.1} 12.0 1 1 0.995
do_wait time 1.0
sound play magic_b 1
sq_screenvibe kawumm
lightning [parse_pos Blitz4] [parse_pos Blitz3] {0.1 0 0} 12 1 0 0.5
lightning [parse_pos Blitz4] [parse_pos Blitz3] {0 0 0} 12.0 0 0 1
lightning [parse_pos Blitz4] [parse_pos Blitz3] {0 0 0.1} 12.0 0 1 0
do_wait time 1.0
sound play magic_b 1
sq_screenvibe kawumm
lightning [parse_pos Blitz5] [parse_pos Blitz6] {0.2 0 0} 12.0 1 0 0
lightning [parse_pos Blitz5] [parse_pos Blitz6] {0 0 0} 12.0 0 0 1
lightning [parse_pos Blitz5] [parse_pos Blitz6] {0 0 0.2} 12.0 0 1 0
sq_screenvibe equake4
do_wait time 4
sq_sound play equake7 1
sq_screenvibe equake7
set_visibility [Object 1] 1; set_anim [Object 1] l1.standard 0 2
set_particlesource [Actor 0] 3 1
do_wait time 1
set_particlesource [Actor 0] 4 1
set_visibility [Object 2] 1; set_anim [Object 2] l1.standard 0 2
do_wait time 1
set_particlesource [Actor 0] 5 1
set_visibility [Object 3] 1; set_anim [Object 3] l1.standard 0 2
do_wait time 1
set_particlesource [Actor 0] 6 1
do_wait time 3
sound play c4_explode1 1
start_fade 2 0
do_wait time 2
sq_pen set EndTalkPos ungefaehrAufschlag
sq_pen move EndTalkPos {12 -9.5 -3}

sq_object summon Zwerg EndTalkPos 6
#4
sq_actor find Zwerg 100 1 6 EndTalkPos
#5
sq_object summon Dummy_Muetze_kampf_02_a
#6
sq_object summon Schwert

link_obj [Object 5] [Actor 1] 11
link_obj [Object 6] [Actor 1] 0
set_roty [Actor 1] 0.75
sq_camera fix EndTalkPos 0.7 -0.25 0
do_wait time 2
start_fade 2 1
change_particlesource [Actor 1] 1 8 {-2 -1 2} {0.2 0 0 } 255 16 0 0 0 0
change_particlesource [Actor 1] 2 8 {-2 -1 2} {0.3 0.05 0 } 255 16 0 0 0 0

do_wait time 1
sq_camera selset inout
sq_camera move EndTalkPos 0.65 -0.25 0 0.2
do_action anim protecteyesstart 1
set_particlesource [Actor 1] 1 1
set_particlesource [Actor 1] 2 1
sq_actor actionlist 1 { {anim protecteyesloop} {anim protecteyesloop} {anim protecteyesloop} {anim protecteyesend } {anim breathe} {anim mann.schwert_c } {anim swordupstart} {anim sworduploop} {anim sworduploop} {anim sworduploop} {anim sworduploop}}
do_wait time 2
set_particlesource [Actor 1] 1 0
set_particlesource [Actor 1] 2 0
do_wait time 5
start_fade 2 0
do_wait time 3

