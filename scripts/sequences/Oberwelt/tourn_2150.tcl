#CLIP 2150 - WETTKAMPF - WIGGLES TRIBÜNE
# Knocker und Wiggle verlassen die Höhle
sq_text file Tournament
sq_audio open Clip_2150

+sq_pen set TribuenePos [Getobjpos Koenig 0 250]
+sq_pen sety TribuenePos 30.5

sq_actor express 0 good_normal

#Vom Wiggle Actor 0 aus, steile Kamerafahrt auf den oben kletternden Knocker der dann
#raussteigt...

set autoidling 0

+sq_wait none
+sq_pen set KletterRaufPos 0
+sq_pen move KletterRaufPos {0 -15 0}
+sq_pen set endpos TribuenePos
+sq_pen setz endpos 9
#sq_camera selset inout
sq_camera follow 0 1.1
do_action run KletterRaufPos 0
do_wait camera
do_wait time 2
sq_camera selset inout
sq_camera move KletterRaufPos 0.7 1 0 0.3

+sq_pen set LeiterEndePos TribuenePos
+sq_pen move LeiterEndePos {36 0.3 5}

+sq_pen set KameraLeiterEndePos TribuenePos
+sq_pen move KameraLeiterEndePos {37 0.3 5}
+sq_pen set LeiterMittePos TribuenePos
+sq_pen move LeiterMittePos {38.3 20 6.7}
+sq_pen set LeiterMitte2Pos TribuenePos
+sq_pen move LeiterMitte2Pos {38.3 12 6.7}
+sq_pen set LeiterObenPos TribuenePos
+sq_pen move LeiterObenPos {38.1 2 6.7}
+sq_pen set Knocker1Pos TribuenePos
+sq_pen move Knocker1Pos {38.1 3 6.5}

+sq_pen set TribueneWigglePos TribuenePos
+sq_pen move TribueneWigglePos {-0.5 0 5}
+sq_pen set TribueneWiggleEndePos TribuenePos
+sq_pen move TribueneWiggleEndePos {-2 0 5}
+sq_pen set TribueneKnockerPos TribuenePos
+sq_pen move TribueneKnockerPos {3 0 5}
+sq_pen set TribueneKnockerEndePos TribuenePos
+sq_pen move TribueneKnockerEndePos {2 0 5}

+sq_pen set TuerSonnePos TribuenePos
+sq_pen move TuerSonnePos { 33.5 0 0 }
+sq_pen setz TuerSonnePos 13

+sq_pen set Sonne1Pos TribuenePos
+sq_pen move Sonne1Pos {16 0 0}

+sq_pen set ElfPos TribuenePos
+sq_pen move ElfPos {-4 0 6}
+sq_pen set ElfBeamPos TribuenePos
+sq_pen move ElfBeamPos {-10 -6 6}

+sq_pen set ZielWiggle1Pos TribuenePos
+sq_pen move ZielWiggle1Pos {14.6 0 4.5}
+sq_pen set ZielKnocker1Pos TribuenePos
+sq_pen move ZielKnocker1Pos {10.0 0 4.5}
+sq_pen set ZielBandPos TribuenePos
+sq_pen move ZielBandPos {5.3 0 4.5}
+sq_pen set Kamera1ZielBandPos TribuenePos
+sq_pen move Kamera1ZielBandPos {8 0 4.5}
+sq_pen set Kamera2ZielBandPos TribuenePos
+sq_pen move Kamera2ZielBandPos {9 0 4.5}
+sq_pen set TrophyPos TribuenePos
+sq_pen move TrophyPos {7.1 0 5}
+sq_pen set NimmTrophyPos TribuenePos
+sq_pen move NimmTrophyPos {7.4 0 5}


#Object 0
+sq_object summon Zwerg Knocker1Pos 2
+call_method [Object 0] Editor_Set_Info {{name Knock} {gender male}}
+call_method [Object 0] init
set_anim [obj_query [Getobjref Zwerg 1 2] "-class Tuer_kaserne -range 20 -limit 1"] tuer_kaserne.oeffnen_b 0 1
do_wait time 0.2

#do_action beam LeiterMittePos 0
sq_actor find Zwerg 20 1 2 Knocker1Pos
do_wait time 0.5
do_change muetze sparetime 1 auf
#do_action run LeiterEndePos 0
do_wait time 0.5
+sq_object summon Dummy_Obw_goldhamster Knocker1Pos
+link_obj [Object 1] [Actor 1] 0
do_wait time 0.3

#Zeit der Kamerafahrt, in der Zeit muß der Knocker erschaffen sein, damit
#er sichtbar wird.
do_wait time 0.5
start_fade 0.5 0
do_wait time 1
sq_camera selset inout
sq_pen set temp 1
sq_pen move temp { 0 -2 0 }
sq_camera move temp 0.9 0.7 0 0.3
do_wait time 1

do_action transport TuerSonnePos 1
start_fade 1 1
do_wait time 4
sq_camera selset inout
sq_camera move KameraLeiterEndePos 1.0 -0.23 0.2 0.3
do_wait time 3

#Der Knocker kommt mit der Trophäe aus dem Schacht geklettert.

##Er will los, aber hält sich plötzlich die Hand vor die Augen - kann nichts mehr sehen.
sq_wait none
#sq_actor express 1 normal_tired
#link_obj [Object 1]; set_rotx [Object 1] 0; set_roty [Object 1] 0; set_rotz [Object 1] 0; set_physic [Object 1] 1
#do_action anim protecteyesstart
gametime factor 1.5
do_action transport TuerSonnePos 1
#do_action run LeiterObenPos 0
do_wait time 4
set_autolight [Actor 0] 0
do_action beam Knocker1Pos 0
do_wait time 1
+gametime factor 1.0
sq_wait none
do_action run TuerSonnePos 0
do_wait time 6
do_action beam ZielKnocker1Pos 1
do_wait time 2

#############################################
#Szene vor dem Zielband und vor der Tribuene
#fade out

sq_camera fix Kamera1ZielBandPos 1.3 -0.2 -0.65 0.07
sq_wait none
do_action beam ZielWiggle1Pos 0
do_wait time 0.2

#+sq_object summon Dummy_Obw_goldhamster ZielKnocker1Pos
#+link_obj [Object 1] [Actor 1] 0

do_action transport TribueneKnockerPos 1
do_action flee TribueneKnockerPos 0
#sq_camera fix Kamera2ZielBandPos 1.1 -0.25 0.4 0.07
do_wait time 2.5
sq_camera move Kamera1ZielBandPos 0.9 -0.2 -0.65 0.07 0.4
do_wait time 1.3

do_action anim mann.treten_hintern 0
do_wait time 0.4

action [Actor 1 ] wait 0
set_anim [Actor 1] standbackhith 0 1
do_wait time 0.1
sq_actor express 1 bad_normal
do_action run NimmTrophyPos 0
link_obj [Object 1]
set_roty [Object 1] -0.5
set_rotz [Object 1] 0.5
sq_object beam 1 TrophyPos
do_wait time 1.2

set_anim [Actor 1] standbackhith 14 0
do_wait time 0.3
do_action anim takeboxa 0
do_wait time 0.1
+link_obj [Object 1] [Actor 0] 0
do_wait time 0.3
do_action transport TribueneWigglePos 0
#Actor 2
sq_actor find Koenig 30 1 any TribuenePos
do_wait time 2
do_action rotate 0 2
sq_actor actionlist 1 {{anim waschface} {anim scratchhead}}
do_action anim standup 1
do_wait time 1
set_anim [Getobjref Dummy_Ziel] ziel.anim 0 1
do_action rotate 0.15 2
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
do_wait time 3
sq_camera fix TribuenePos 1.3 -0.2 0 0.4
do_elf beam ElfBeamPos
do_elf move ElfPos
#Actor 3-6
do_action rotate 1 2
sq_actor eyes 2 { u u u u u u 9 u u u u u u u u 9 u u u u u u u u u u 9 u u u u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u 9 u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u 9 u u u u u u u u u u 9 u u u u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u 9 u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u}
sq_actor find Trompeter 30 2 any TribuenePos
do_wait time 2
do_action rotate 0.3 2
do_action walk TribueneKnockerEndePos 1
do_wait time 4

##Der Wiggle kommt angerannt, die Elfe folgt ihm aufgeregt. Nur noch ein paar Schritte bis zum Knocker... Der jetzt die Ziellinie mit dem wartenden König erreicht hat...
##Der König packt den Wiggle-Arm und reißt ihn in die Luft!
sq_camera selset inout
+link_obj [Object 1]
do_wait
+set_rotx [Object 1] 0; set_roty [Object 1] 0;  set_rotz [Object 1] 0
+set_pos [Object 1] [parse_pos endpos]
+set_physic [Object 1] 1
sq_camera fix 2 0.8 0.1 0
do_action rotate front 0
sq_actor actionlist 0 {{anim bowlwin} {anim standloopa} {anim cheer} {anim cheer} {anim cheer} {anim bowlwin}}
do_action rotate front 0
#do_text "Es ist ein Clan gefunden.... diiieeee WIIIIGGGGLLLEESSS!" 2;# {{hammerstart} {hammerloop} {hammerloop} {hammerend}}
do_text 2150aa 2 {talkc} Es
do_wait time 2
do_text 2150ab 2 {dontknow} Die
#-sound play jubel_4o 1
-if {[get_objgender [Actor 0]]=="male"} {sound play jubel_4o 1} else {sound play jubel_4x 1}
do_wait time 2
#-sound play jubel_4o 1
-if {[get_objgender [Actor 0]]=="male"} {sound play jubel_4o 1} else {sound play jubel_4x 1}
do_wait time 5
sq_camera fix 0 0.8 -0.2 0
do_wait time 1

sq_actor actionlist {3 4} {{anim fanfare} {anim fanfare}}
do_action anim fanfare 3
do_action anim fanfare 4
do_action walk TribueneKnockerEndePos 1
do_elf anim salto
do_wait time 2
do_action rotate 1 0
do_action rotate 0 1
do_wait time 1.5

sq_camera fix TribueneKnockerEndePos 0.8 -0.2 0 0.4
do_action rotate 1 2
do_text 2150b 1 {{talkc} {talkc}} Schiebung
do_action walk TribueneWiggleEndePos 0
do_wait time 2.6
do_action rotate 1 0
do_action rotate 0 1
sq_camera fix TribuenePos 1.3 -0.2 0 0.4
do_elf lookat 1
do_text 2150c 1 {{swordtwist} {swordsalut}} Dat
do_action rotate 0 2
do_action run TribueneWiggleEndePos 0
do_wait time 0.4

#do_action anim putboxa 0
do_wait

+set_attrib [Actor 0] exp_F_Sword 0.1
+set_attrib [Actor 1] exp_F_Sword 0.1
do_wait time 1.0

do_action rotate right 0
do_wait time 0.5

+do_action beam TribueneWiggleEndePos 0
do_wait time 0.5
do_action rotate front 2
+foreach item [inv_list [Actor 0]] { inv_rem [Actor 0] $item; del $item }

+do_action beam TribueneKnockerEndePos 1
do_wait time 1
+sq_object summon Schwert
do_wait time 0.4
+inv_add [Actor 0] [Object 2]
do_wait time 0.3
+sq_object summon Schwert
do_action anim dontknow 2
do_wait time 0.3
+inv_add [Actor 1] [Object 3]
do_wait time 0.3
+sq_camera fix TribuenePos 1.3 -0.2 0
+sq_camera get
+set_autolight [Actor 0] 1
+sel [Actor 1]; set_event [Actor 1] evt_task_attack -target [Actor 1] -subject1 [Actor 0]
+adaptive_sound changetheme tournament


