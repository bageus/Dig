+sq_object delete all
do_wait time 3
sq_pen set P1 	[Getobjpos Info_Pos_Zwerg]
sq_pen set PX1 	[Getobjpos Info_Pos_Zwerg]
sq_pen set P2 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen set PP2 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen set Odin1	[Getobjpos Info_Pos_Zwerg]
sq_pen set Odin2	[Getobjpos Info_Pos_Zwerg]
sq_pen move P1 {-11 0 -2}
sq_pen move P2 {6.5 0 -2}
sq_pen move PP2 {6.5 -3 -2}
sq_pen move C1 {-13 0 -4}
sq_pen move Odin1 {-10 -3 0}
sq_pen move Odin2 {-10 -7 0}
sq_camera fix C1 1.38 -0.355 0.165
do_particle create 13 P1 {0 -0.15 0} 100 2
do_particle create 18 P1 {0 -0.15 0} 100 2
do_particle create 12 P1 {0 -0.25 0} 100 2
do_particle create 22 P1 {0 -0.10 0} 100 2
do_particle create 7 P1 {0 -0.10 0} 100 2
do_particle create 13 P1 {0 -0.05 0} 100 2
do_particle create 13 P1 {0 -0.18 0} 100 2
do_wait time 0.5
sq_object summon Troll P1
do_wait time 0.2
sq_actor find Troll
sq_wait 0
set_anim [Actor 0] troll.stehen_zaubern_b 8 1
do_wait time 2
do_action rotate P2 0
sq_wait none
do_action anim troll.stehen_zaubern_a 0
do_wait time 1
do_particle create 13 P2 {0 -0.15 0} 100 2
do_particle create 18 P2 {0 -0.15 0} 100 2
do_particle create 12 P2 {0 -0.25 0} 100 2
do_particle create 22 P2 {0 -0.10 0} 100 1.5
do_particle create 7 P2 {0 -0.10 0} 100 2
do_particle create 13 P2 {0 -0.05 0} 100 2
do_particle create 3 P2 {0 -0.18 0} 100 2
do_wait time 0.5
sq_object summon Troll P2
do_wait time 0.2
sq_actor find Troll
set_anim [Actor 1] troll.stehen_versteinert_tot 4 1
do_wait time 1.5
sq_wait 1
do_action rotate P1 1
sq_wait none
do_action anim troll.spies_anschreien_b 1
do_wait time 0.3
do_action anim troll.stehen_zaubern_c 0
do_wait time 0.5
do_particle create 11 PP2 {0 0 0} 100 5
do_wait time 1
do_particle create 22 PP2 {0 0 0} 100 5
do_wait time 1
sq_wait 1
do_action anim troll.stehen_brennen 1
do_action anim troll.stehen_brennen_reanim 1
do_action anim troll.stehen_drohen 1
sq_wait none
do_action anim troll.stehen_zaubern_b 1
do_wait time 0.5

do_particle create 13 P1 {0 -0.15 0} 100 2
do_particle create 18 P1 {0 -0.15 0} 100 2
do_particle create 12 P1 {0 -0.25 0} 100 2
do_particle create 22 P1 {0 -0.10 0} 100 1.5
do_particle create 7 P1 {0 -0.10 0} 100 2
do_particle create 13 P1 {0 -0.05 0} 100 2
do_particle create 3 P1 {0 -0.18 0} 100 2
do_wait time 0.4
sq_object delete 0
do_wait time 0.2
sq_object summon Hamster P1
do_wait time 1
sq_actor find Hamster
set_anim [Actor 2] hamster.maennchen 0 0
do_wait time 2
sq_wait 1
do_action anim troll.stehen_tanzen_start 1
do_action anim troll.stehen_tanzen_loop 1
do_action anim troll.stehen_tanzen_end 1
sq_wait none
do_action anim troll.stehen_zaubern_a 1
do_wait time 1
do_particle create 13 P1 {0 -0.15 0} 100 2
do_particle create 18 P1 {0 -0.15 0} 100 2
do_particle create 12 P1 {0 -0.25 0} 100 2
do_particle create 22 P1 {0 -0.10 0} 100 1.5
do_particle create 7 P1 {0 -0.10 0} 100 2
do_particle create 13 P1 {0 -0.05 0} 100 2
do_particle create 3 P1 {0 -0.18 0} 100 2
do_wait time 0.4
sq_object delete 1
do_wait time 0.2
sq_object summon Troll P1
do_wait time 1
#do_action run PX1 3
sq_object summon Hand_Gottes Odin2
set_roty [Object 2] 0.6
sq_object move 2 Odin1 0.3
set_anim [Object 2] tabnose 0 1
do_wait time 3
do_particle create 13 P1 {0 -0.15 0} 100 2
do_particle create 13 P2 {0 -0.15 0} 100 2
do_particle create 18 P1 {0 -0.15 0} 100 2
do_particle create 18 P2 {0 -0.15 0} 100 2
do_particle create 12 P1 {0 -0.25 0} 100 2
do_particle create 12 P2 {0 -0.25 0} 100 2
do_particle create 22 P1 {0 -0.10 0} 100 1.5
do_particle create 22 P2 {0 -0.10 0} 100 1.5
do_particle create 7 P1 {0 -0.10 0} 100 2
do_particle create 7 P2 {0 -0.10 0} 100 2
do_particle create 13 P1 {0 -0.05 0} 100 2
do_particle create 13 P2 {0 -0.05 0} 100 2
do_particle create 3 P1 {0 -0.18 0} 100 2
do_particle create 3 P2 {0 -0.18 0} 100 2
do_wait time 0.7

#Ende SEQ

+sq_object delete all
do_wait time 6





