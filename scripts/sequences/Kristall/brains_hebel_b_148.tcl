start_fade 0.5 0
do_wait time 0.5
sq_wait none
sq_text file Kristall
sq_audio open Kris_148
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmokristall
#-----------------------------------------
sq_actor find Zwerg 10 1 0
sq_actor find Riesenlaufrad 100 1

#---------------------------------
#sq_actor find Ring_Der_Magie
#sq_pen set alterring 3
do_wait
#---------------------------------

+sq_pen set RingAltPos TriggerPos
sq_object summon Ring_Der_Magie RingAltPos
do_wait
sq_actor find Ring_Der_Magie
set_physic [Object 0] 0
+sq_pen set WigglesPos RingAltPos
+sq_pen move WigglesPos { 0.8 -0.7 0.6 }
+sq_pen set Wiggles2Pos RingAltPos
+sq_pen move Wiggles2Pos { 0.8 0 2 }
+sq_pen set Wiggles3Pos RingAltPos
+sq_pen move Wiggles3Pos { 2 0 8 }
+sq_pen set Ring3Pos RingAltPos
+sq_pen move Ring3Pos { 2 -1.0 8 }
+sq_pen set Ring4Pos RingAltPos
+sq_pen move Ring4Pos { -3 -3.0 6 }
+sq_pen set Ring5Pos RingAltPos
+sq_pen move Ring5Pos { -3 -2.0 6 }
+sq_pen set Wiggles4Pos RingAltPos
+sq_pen move Wiggles4Pos { -4 0.5 6 }
+sq_pen set wech RingAltPos
+sq_pen move wech { 50 0.5 2 }
+sq_pen set RingNeuPos RingAltPos
+sq_pen move RingNeuPos { 1 1 0 }
+sq_pen set Cam01 RingAltPos
+sq_pen move Cam01 { 1 -0.8 0 }
+sq_pen set Cam02 RingAltPos
+sq_pen move Cam02 { -0.5 -0.9 0 }
+sq_pen set Cam03 RingAltPos
+sq_pen move Cam03 { 0.5 0 0 }
+sq_pen set ElfeStart RingAltPos
+sq_pen move ElfeStart { 10 -0.5 10 }
+sq_pen set ElfeSprech RingAltPos
+sq_pen move ElfeSprech { 0 0 7 }
+sq_pen set RadPos [Getobjpos Riesenlaufrad]
+sq_pen move RadPos { 0 -1 0}
+sq_pen set HamsterPos [Getobjpos Riesenlaufrad]
+sq_pen move HamsterPos { -2 0 0}
+sq_pen setz HamsterPos 13
+sq_pen set RingPos HamsterPos
+sq_pen move RingPos { -1 -2 0 }
sq_actor find Riesenhamster 100 1
do_wait
sq_color 1 Wiggle1
link_obj [Object 0] [get_ref this] 0
do_action beam wech 3
do_action beam WigglesPos 1
do_action rotate back 1
do_wait time 1
sq_actor actionlist 1 { { anim climbstillani } loop }
sq_camera selset inout
do_elf beam ElfeStart
sq_object beam 0 RingNeuPos
sq_camera fix Cam01 1.2 -0.1 0.3
do_action anim climbstillani 1
do_wait time 1
start_fade 2 1
do_elf lookat 1
sq_camera move Cam01 0.7 -0.1 0.3 0.3
do_wait time 2
do_elf move ElfeSprech
do_wait time 3
sq_camera fix Cam02 0.67 -0.1 -0.9
do_wait time 1
do_action anim takewall 1
sq_wait elf
sq_sound 148a 1
do_elf text 0148a {warnen}
sq_wait none
do_text 0148b 1 { climbhita climbstillani climbhita } 148b
do_wait time 2
do_action anim takewall 1
do_wait time 0.2
link_obj [Object 0]
do_action beam wech 3
do_action anim toolputawaywall_a 1
do_wait time 0.2
do_action anim toolputawaywall_b 1
do_wait time 0.2
do_wait time 0.4
screenvibe 20 0.4 0.4 0.04 103 0.04 200
sq_camera move Cam03 0.9 -0.1 0.0 0.2
sq_wait 1
sq_actor actionlist 1 {}
do_action walk Wiggles2Pos 1
sound play brainmasch_kaputt_end 1
do_action wait 1.5 1
sq_actor eyes 1 { c 4 4 4 4 4 4 4 4 4 4 4 4 4 4 c }
do_action wait 0.7 1
do_action anim scratchhead 1
sq_wait elf
sq_sound 148c 1
do_elf text 0148c {auffordern}
sq_wait none
+do_elf hide
do_wait time 0.5
sq_actor eyes 1 { c 4 o o 4 4 l l l l l l c c c 3 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 }
do_action wait 3 1
do_wait time 1
sq_wait none
do_action flee Wiggles3Pos 1
sound play Riesenlaufrad_kaputt 1
do_wait time 1
do_action anim stummble 1
sound play c4_explode1 1
screenvibe 0	0.15 	0.4 	0.3 	100 	0.3 	114
set_anim [get_ref this] kris_maschine.standard 0 2
+change_particlesource [get_ref this] 1 18 { 0 0 0 } { -0.03 -0.05 0.2 } 100 5 0 1 2
+change_particlesource [get_ref this] 2 6 { 0 0 0 } { 0 0 0 } 200 3 0 1

#+change_particlesource [get_ref this] {3-6} 12 { 0 0.5 0 } { 0 -0.05 0 } 100 3 0 {2-5} 2.5

+change_particlesource [get_ref this] 7 0 { -0.1 0.3 0.5 } { 0 -0.05 0 } 100 2 0 6 1
+change_particlesource [get_ref this] 8 6 { -0.1 0.3 0.5 } { 0 -0.05 0 } 100 3 0 6 2

+change_particlesource [get_ref this] 9 6 { 0 0.5 0.5 } { 0 0 0 } 100 5 0 12 2
+change_particlesource [get_ref this] 10 6 { 0 0 0.5 } { 0 0 0 } 100 2 0 7 2

+change_particlesource [get_ref this] 11 12 { 0 0 0 } { 0.03 -0.04 0.1 } 100 1 0 8 3
+change_particlesource [get_ref this] 12 6 { 0.4 0 0 } { 0 0 0 } 100 2 0 8 2

+change_particlesource [get_ref this] 13 18 { -1 -1.2 0.9 } { 0 0 0.05 } 100 10 0 9 2
+change_particlesource [get_ref this] 14 6 { -1.3 -1.4 1.2 } { 0 0 0.05 } 100 7 0 9 2

+change_particlesource [get_ref this] 15 18 { 0 0 0 } { 0.03 0 0.05 } 100 7 0 11 2
+change_particlesource [get_ref this] 16 18 { 0 0 0 } { -0.03 0 0.05 } 100 3 0 11 2
+change_particlesource [get_ref this] 17 6 { 0 0 0 } { 0 0 0.05 } 100 2 0 11 2

+set_particlesource [get_ref this] 1 1
+set_particlesource [get_ref this] 2 1
+set_particlesource [get_ref this] 7 1
+set_particlesource [get_ref this] 8 1
+set_particlesource [get_ref this] 9 1
+set_particlesource [get_ref this] 10 1
+set_particlesource [get_ref this] 11 1
+set_particlesource [get_ref this] 12 1
+set_particlesource [get_ref this] 13 1
+set_particlesource [get_ref this] 14 1
+set_particlesource [get_ref this] 15 1
+set_particlesource [get_ref this] 16 1
+set_particlesource [get_ref this] 17 1



sq_wait none
do_action beam Ring3Pos 3
sq_object move 0 Ring4Pos 0.2
do_wait time 1.5
sq_object move 0 Ring5Pos 0.2
do_wait time 3
#Riesenlaufrad (Actor 2) wird zerstört
+call_method [Actor 2] destroy
#Riesenhamster (Actor 4) wird positioniert
+sq_object delete all
+set_anim [Actor 4] riesenhamster.standanim 0 2
+do_action beam HamsterPos 4
+ringplatzieren [parse_pos RingPos]
do_wait time 1
set_physic [Object 0] 1
sq_camera selset inout
sq_pen move RingPos { 0 2 0 }
sq_camera move RingPos 0.8 -0.1 -0.9 0.3
do_wait time 4
sq_camera move HamsterPos 1.1 -0.1 -0.5 0.2
do_wait time 1.5
do_action anim riesenhamster.schlagen 4
+set_pos [Actor 1] [parse_pos Wiggles3Pos]
do_wait time 2
sq_camera fix 1 1.0 -0.2 0.7
do_action rotate left 1
do_wait time 1
sq_actor eyes 1 { c 3 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 }
do_action anim walkback 1
do_wait time 2
+do_wait
+cancel_fade
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changetheme brains
+get_angry
#-----------------------------------------

