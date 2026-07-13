# WETTKAMPF STARTET
start_fade 0.1 0
sq_text file Tournament
sq_audio open Obw_2100
#+sq_actor find Zwerg 100 3 0
global actors; log "Actors: $actors"
set_collision [obj_query this -class Info_Coll_b -boundingbox {-1 -1 -14 2 1 -4} -limit 1] 0
global actors;foreach gnome $actors {set_particlesource $gnome 1 0;ref_set $gnome current_occupation idle}

+option set showUI 1
+sq_activate Zuschauer range 400 16

+sq_actor find Koenig 300 1
#Voodoos 5/6
+sq_actor find Zwerg 100 2 1
#Knockers 7/8
+sq_actor find Zwerg 100 2 2
#Brains 9/10
+sq_actor find Zwerg 100 2 3

sq_pen set Cam_P1 TriggerPos
sq_pen set Cam_P2 TriggerPos
sq_pen set Cam_P3 TriggerPos
sq_pen set P_Koenig [Getobjpos Koenig 7 400]
sq_pen set P_Trom1 P_Koenig
sq_pen set P_Trom2 P_Koenig
sq_pen set Wiggles1_0 TriggerPos
sq_pen set Wiggles1_1 TriggerPos
sq_pen set Wiggles2 TriggerPos
sq_pen set Wiggles3_0 TriggerPos
sq_pen set Wiggles3_1 TriggerPos
sq_pen set P_Elfe TriggerPos

sq_pen set Brains TriggerPos
sq_pen set Knockers TriggerPos
sq_pen set Vampy TriggerPos
sq_pen set Voodoos TriggerPos

+sq_pen set WalkStart TriggerPos
+sq_pen move WalkStart {-60 0 5}
+sq_pen setz WalkStart 11
+sq_pen set WalkDest WalkStart
+sq_pen move WalkDest {18 0 0}
+sq_pen form WalkStart Circle 5
+sq_pen form WalkDest Circle 5
do_action beam WalkStart { 0 1 2 3 }

sq_pen move Wiggles1_0 { -1.5 0 0 }
sq_pen move Wiggles1_1 { 1.5 0 0 }
sq_pen move Wiggles2 { -5 0 -10 }
sq_pen move Wiggles3_0 { -1 0 -10 }
sq_pen move Wiggles3_1 { -2 0 -10 }
sq_pen move P_Elfe { -2 -1.5 0 }

sq_pen move Brains { 8 0 0 }
sq_pen move Voodoos { 16 0 0 }
sq_pen move Knockers { 24 0 0 }
sq_pen move Vampy { -8 0 0 }


sq_pen move Cam_P1 { -10 -2 -6 }
sq_pen move Cam_P3 { 10 -4 0 }
sq_pen move P_Trom1 { -4 -1 0 }
sq_pen move P_Trom2 { 4 -1 0 }

#Trompeter 11/12
sq_actor find Trompeter 100 2
do_wait
foreach t [obj_query [Actor 0] -class Info_Coll_c -range 50 -limit 2] {set_collision $t 0}

set_view 139.2 29.4 1.5 -0.46 -0.04
do_wait time 0.5
sq_color 7 {201 187 130}
sq_color 8 {188 158 126}
sq_wait none
sq_actor actionlist { 2 3 } { { { anim standloopa } { anim scratchhead } { anim scout } { anim showleft } { anim showright } } }
sq_actor actionlist { 0 1 } { { { anim standloopa } { anim scratchhead } { anim scout } { anim showleft } { anim showright } } }
do_action run WalkDest { 0 1 2 3 }
+start_fade 5 1
sq_wait { 0 1 2 3 }
do_wait
sq_wait none
+sq_pen move WalkDest {30 0 0}
+sq_pen form WalkDest Circle 5
do_action flee WalkDest { 0 1 2 3 }

do_change muetze sparetime { 5 6 7 8 9 10 } auf

sq_object summon Dummy_Trompete
link_obj [Object 0] [Actor 11] 0
sq_object summon Dummy_Trompete
link_obj [Object 1] [Actor 12] 0

sq_object summon Dummy_Joint_a
sq_object summon Halbzeug_Taschenrechner
sq_object summon Spitzhacke
sq_object summon Spitzhacke
sq_object summon Spitzhacke
sq_object summon Spitzhacke

sq_camera addset inspeed 0.0 {{ s 1.0 1.0}}
sq_camera addset samespeed 1.0 {{ s 1.0 1.0 }}
sq_camera addset outspeed 1.0 {{ s 1.0 0.0}}

#---inserted-by-Jan---MUSIC---------------
adaptive_sound markerdisable
adaptive_sound changethemenow tournstart
#-----------------------------------------

sq_camera selset inspeed
sq_camera move 0 1.3 -0.3 -0.15 0.15
do_wait camera

sq_camera selset samespeed
sq_camera move Cam_P1 1.7 -0.4 0.6 0.15
do_wait time 3

sq_camera move Cam_P2 1.3 -0.5 0.2 0.15
do_wait time 2

sq_camera move Cam_P3 1.6 -0.3 -0.1 0.15
do_wait time 2

do_action beam Wiggles2 1
sq_pen move Wiggles2 { 1.2 0 0 }
do_action beam Wiggles2 0

sq_camera selset outspeed
sq_camera move P_Koenig 2.0 -0.1 -0.3 0.2
do_elf beam P_Elfe
do_wait camera

sq_wait 4
sq_camera move P_Koenig 0.9 -0.2 -0.3
do_action rotate front 4
sq_actor eyes 4 { c c l l l l l l 9 l l l l 9 l l l l l l l l l l l 9 4 4 4 4 4 4 4 4 4 c }
do_text 2100a 4 Auto a
sq_camera fix TriggerPos 1.0 -0.1 0.165
sq_wait 1
sq_actor actionlist 0 { {anim tired} {anim breathe} }
do_action flee Wiggles1_0 0
sq_actor actionlist 1 { {anim breathe} {anim tired} }
do_action flee Wiggles1_1 1
sq_camera fix P_Koenig 0.8 -0.1 0.6
sq_wait 4
sq_sound raeusper 4
do_action rotate 0.9 4
do_action anim cough 4
sel /obj;new Dummy_Obw_absperrung_c "" {179.12 30.5 2.99} {0 0 0}
do_wait time 1
do_text 2100b 4 Auto b
sq_pen move Cam_P3 { 7 0 0 }
sq_wait none
sq_camera fix P_Koenig 1.5 -0.1 0.32
do_text 2100c 4 Auto c
do_wait time 4
sq_camera fix P_Koenig 0.9 -0.15 -0.165
sq_wait 4
do_wait
sq_camera fix Brains 1.1 -0.2 0.1
sq_wait none
sq_actor eyes 10 { c o o o o o o o o c c c c c c c c c c 9 9 9 9 c c }
sq_actor actionlist 10 { {anim scratchhead} {anim discod} }
sq_wait 9
do_action anim tooltakeout_a 9
link_obj [Object 3] [Actor 9] 0
do_action anim tooltakeout_b 9
sq_actor eyes 9 { c c c c c c c c c u u u u u u u 1 1 1 3 3 3 3 3 3 3 3 u u u u u u c }
sq_actor actionlist 9 { {anim calculator} {anim scratchhead} }
sq_wait none
do_action anim teeter_t 10
sq_wait 9
do_action anim toolputaway_a 9
link_obj [Object 3]
sq_object beam 3 { 0 0 0 }
#sq_object delete 3
link_obj [Object 2] [Actor 5] 0
do_action anim toolputaway_b 9
sq_camera selset inout
sq_camera move Voodoos 1.1 -0.2 0.1 1
sq_actor actionlist 6 { {{anim scratchhead} {anim standloopb} {anim standloopc} {anim standloopa}} loop}
sq_wait 5
do_action anim standloopa 6
do_action rotate front 5
sq_actor eyes 5 {c c c c c c c c c c c 5 5 5 5 5 5 5 5 c c 5 5 5 5 5 5 5 c 5 c 5 5 c}
sq_actor mouth 5 {c c c c c c c c c c c 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 c}
do_action anim teeter_w 5
do_action anim standloopc 5
do_action anim teeter_w 5
do_action anim teeter_w 5
do_action anim standloopb 5
do_action rotate 4 5
sq_wait none
do_action anim smokepotstop 5
do_wait time 0.4
link_obj [Object 2]
sq_object beam 2 { 0 0 0 }
#sq_object delete 2
do_wait time 1
sq_actor actionlist {7 8} { { {anim scout} {anim wait} {anim teeter_w} } loop}
do_action rotate 4 7
sq_wait none
do_action rotate 4 8
sq_camera move Knockers 1.1 -0.2 0.1 1
do_wait time 3
sq_actor actionlist {7 8} { { {anim wait} {anim wait} {anim teeter_w} } loop}
sq_camera fix TriggerPos 1.1 -0.2 -0.3
sq_actor actionlist 0 { {anim handstandloop} {anim handstandloop} {anim handstandloop} {anim handstandloop} {anim handstandloop} {anim handstandloop} {anim handstandstop} {anim teeter_t} }
do_action anim handstandstart 0
do_wait time 0.3
sq_wait 0
elf unfollowview
do_elf anim ablehnen
do_wait time 1
sq_camera move Vampy 1.4 -0.2 -0.3 0.5
do_wait time 4
sq_camera fix P_Koenig 0.7 0.1 0.165
sq_wait 4
do_action rotate front 4
do_text 2100d 4 {scratchhead showleft talka} d
do_text 2100e 4 {talkacngb} e
sq_camera fix P_Koenig 0.7 0.2 -0.7
do_action rotate 4 {5 6 7 8 9 10}
do_text 2100f 4 Auto f
sq_camera fix P_Trom1 1.85 -0.1 -0.7
do_wait time 2
do_text 2100g 4 Auto g
sq_camera fix P_Koenig 1.0 -0.3 0.9
do_action rotate Knockers 4
do_text 2100h 4 Auto h
sq_camera fix P_Koenig 0.7 0.2 0.2
do_text 2100i 4 {talkrentb talkrenta} i
do_wait time 2
do_text 2100j 4 {talkd} j

sq_actor actionlist 8 {}
sq_wait 8
sq_camera fix Knockers 1.0 -0.2 0.1 1
sq_actor express { 7 8 } normal_tired
sq_actor actionlist 7 { {anim standloopd} {anim scratchhead} }
do_action anim standloopc 7
do_text 2100k 8 {scout scratchhead} k
do_action rotate front 8
sq_actor actionlist 7 {}
sq_camera fix 7 0.9 -0.1 -0.8
sq_wait 7
+sq_pen set Knocker1 7
+sq_pen move Knocker1 { -1 0 3 }
+sq_pen set Knocker2 8
+sq_pen move Knocker2 { 1 0 2 }
do_action rotate P_Koenig 7
do_text 2100l 7 {protecteyesstart protecteyesloop protecteyesstop talke} l
sq_wait 8
do_action rotate 8 7
do_action rotate 7 8
sq_camera fix 8 0.8 -0.15 0.9
do_text 2100m 8 Auto m
+sq_wait 7
sq_camera fix Knockers 1.2 -0.5 0.1 1
do_text 2100n 7 Auto n
+do_action walk Knocker2 8
+do_action walk Knocker1 7
+add_expattrib [Actor 7] exp_Stein 0.5
+add_expattrib [Actor 8] exp_Stein 0.5
+sq_wait none
+for {set i 205} {$i < 207} {incr i} {for {set j 31} {$j < 42} {incr j} {dig_mark 2 $i $j 1} }

+set_sequenceactive [Actor 7] 1
+set_sequenceactive [Actor 8] 1

+sel [Actor 7];set_event [Actor 7] evt_task_dig -target [Actor 7] -pos1 {205 31}
+sel [Actor 8];set_event [Actor 8] evt_task_dig -target [Actor 8] -pos1 {206 31}

do_wait time 5
sq_wait 4
sq_camera fix P_Koenig 1.2 -0.15 -0.2
do_action rotate front 4
sq_camera move P_Koenig 0.9 -0.15 -0.2
do_text 2100o 4 Auto o
sq_wait none
sq_camera fix Brains 1.3 -0.2 -0.3
sq_pen move Brains { 0 0 6 }
do_action rotate Brains 9
sq_wait 10
do_action rotate Brains 10
do_action anim tooltakeout_a 10
link_obj [Object 4] [Actor 10] 0
do_action anim tooltakeout_a 9
do_action anim tooltakeout_b 10
link_obj [Object 5] [Actor 9] 0
do_action anim tooltakeout_b 9
do_wait time 0.5
sq_camera fix Voodoos 0.9 -0.2 -0.9
sq_pen move Voodoos { 0 0 6 }
sq_actor actionlist 6 {}
sq_wait 5
#--BEWEGUNGSTHERAPIE-DAMIT SIE GRABEN KOENNEN-
+sq_pen set Voodoo1 5
+sq_pen move Voodoo1 { -1 0 3 }
+sq_pen set Voodoo2 6
+sq_pen move Voodoo2 { 1 0 2 }
+do_action walk Voodoo2 6
+do_action walk Voodoo1 5
#---------------------------------------------
do_action rotate Voodoos {5 6}
do_action anim tooltakeout_a 5
link_obj [Object 6] [Actor 5] 0
do_action anim tooltakeout_a 6
do_action anim tooltakeout_b 5
link_obj [Object 7] [Actor 6] 0
do_action anim tooltakeout_b 6
do_wait time 0.5
sq_camera fix Knockers 1.1 -0.125 0.4
do_wait time 4
sq_wait none
sq_camera fix P_Koenig 0.7 -0.3 -0.2
do_text 2100p 4 {cheer} p
do_wait time 0.4
sq_camera fix P_Trom2 1.2 -0.3 -0.2
sq_wait 11
do_action anim fanfare {11 12}
do_action anim fanfare {11 12}
do_action anim fanfare {11 12}
sq_camera fix TriggerPos 1.8 -0.3 0
+sq_actor actionlist { 0 1 5 6 9 10 } {}

#+for {set i 189} {$i < 191} {incr i} {for {set j 31} {$j < 34} {incr j} {dig_mark 3 $i $j 1} }
+for {set i 197} {$i < 199} {incr i} {for {set j 31} {$j < 42} {incr j} {dig_mark 1 $i $j 1} }

+set_sequenceactive [Actor 9] 1
+set_sequenceactive [Actor 10] 1
+set_sequenceactive [Actor 5] 1
+set_sequenceactive [Actor 6] 1

+do_wait time 1

+sq_object delete all

+do_action beam Knocker2 8
+do_action beam Knocker1 7
+do_action beam Voodoo2 6
+do_action beam Voodoo1 5

#+sel [Actor 9];set_event [Actor 9] evt_task_dig -target [Actor 9] -pos1 {189 32}
#+sel [Actor 10];set_event [Actor 10] evt_task_dig -target [Actor 10] -pos1 {190 32}
+sel [Actor 5];set_event [Actor 5] evt_task_dig -target [Actor 5] -pos1 {197 32}
+sel [Actor 6];set_event [Actor 6] evt_task_dig -target [Actor 6] -pos1 {198 32}
+sel [Actor 7];set_event [Actor 7] evt_task_dig -target [Actor 7] -pos1 {204 32}
+sel [Actor 8];set_event [Actor 8] evt_task_dig -target [Actor 8] -pos1 {205 32}



+gametime factor 1
+sq_actor focus all none
+sq_pen set Position TriggerPos
+sq_camera move TriggerPos 1.2 -0.2 0.1
do_wait time 2
+sq_pen move Position {-1 0 0}
+do_action beam Position 0
+sq_pen move Position {2 0 0}
+do_action beam Position 1
+sq_pen move Position {32 0 0}
+set_view 181.5 29 1.2 -0.2 0.1
+do_action beam Position 2
+do_action beam Position 2
+do_action beam Position 2
+global viewpos; set viewpos {181.5 29 1.2 -0.2 0.1}
+do_action beam Position 2
+do_action beam Position 2
+do_action beam Position 2
+sq_pen move Position {0 0 -2}

+do_action beam Position 3
+set_collision [obj_query this -class Info_Coll_b -boundingbox {-1 -1 -14 2 1 -4} -limit 1] 1
+foreach t [obj_query [Actor 0] -class Info_Coll_c -range 50 -limit 2] {set_collision $t 0}
+do_wait
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound markerenable
+adaptive_sound marker tournament [get_pos this]
+adaptive_sound changetheme tournament
#-----------------------------------------
+cancel_fade
+do_wait
