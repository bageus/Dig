#CLIP 2160 Knockerschwert bricht - Ende Wettkampf
#elf say "Sequenz tourn-2160"

#---inserted-by-Jan---MUSIC---------------
#adaptive_sound changethemenow atmotourn
#-----------------------------------------

+sq_text file Tournament
sq_audio open Clip_2160

start_fade 0.5 0

+sq_pen set TribuenePos [Getobjpos Dummy_Obw_tribuene 0 250]
+sq_pen set Wiggle1Pos TribuenePos
+sq_pen move Wiggle1Pos {-0.5 0 6}
+sq_pen set Knocker1Pos Wiggle1Pos
+sq_pen move Knocker1Pos {1 0 0}

+sq_pen set KameraFightPos Wiggle1Pos
+sq_pen move KameraFightPos {0.2 0 5}
#sq_pen move Wiggle1Pos {-0.25 0 5}
+sq_pen set SchwertFlugPos TribuenePos
+sq_pen move SchwertFlugPos {-8 -6 3}
+sq_pen set KnockerRausPos TribuenePos
+sq_pen move KnockerRausPos {8 0 5}


sq_actor find Zwerg 30 1 2 TribuenePos
#do_action beam 0 Wiggle1Pos
do_wait time 0.5
sq_actor find Koenig 30 1 any TribuenePos
do_wait time 0.5

sq_actor express 0 fight_v1
sq_actor express 1 fight_v2

do_action beam Wiggle1Pos 0
do_action beam Knocker1Pos 1
do_wait time 1

#sound play Schwerthiebe

sq_actor actionlist 2 {{anim standloopa} {anim punch} {anim protecteyesstart} {anim protecteyesloop} {anim protecteyesstop} {anim applaud} {anim protecteyesstart} {anim protecteyesloop} {anim protecteyesstop} {anim punch} {anim protecteyesstart} {anim protecteyesloop} {anim protecteyesstop} {anim punch} {anim applaud}}
#sq_actor eyes 2 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 { u u u u u u 9 u u u u u u u u 9 u u u u u u u u u u 9 u u u u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u 9 u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u 9 u u u u u u u u u u 9 u u u u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u 9 u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u}

+sq_camera fix KameraFightPos 0.85 -0.05 0
+start_fade 1 1

do_action rotate 0 1
do_action rotate 1 0
do_wait time 0.5

do_action anim swordtwist 0
do_action anim swordtwist 1
do_action rotate 0 2
do_wait time 0.8
do_action anim swordheadstroke 0
do_action anim swordduck 1
#sq_actor express 2 normal_good
do_wait time 0.8
do_action anim swordjump 0
do_action anim swordbotstab 1
do_wait time 0.8
do_action anim swordstillani 0
do_action anim swordstillani 1
do_wait time 0.5
do_action anim swordmidstroke 0
do_action anim swordside 1
do_wait time 0.5
do_action anim swordheadstroke 1
do_action anim swordback 0
do_wait time 0.8
gametime factor 0.2
do_action anim swordmidstroke 1
do_action anim swordback 0
do_wait time 0.4
do_action anim swordmidstroke 1

do_wait time 0.1
do_action anim swordheadhith 1
#do_wait time 0.0
do_action anim swordmasterstroke 0

#create	<nr des Effektes>	<pos/pen/actor>	<vector>	<Anzahl>	<Dauer>

+sq_pen set BlutPos 1
+sq_pen move BlutPos {0 -0.5 0}
set_anim [Actor 1] swordheadhith 0 1
do_wait time 0.2
do_particle create 8 1 {-0.01 -0.32 0} 5 0.5
do_particle create 8 1 {-0.02 -0.28 0} 5 0.5
do_particle create 8 1 {-0.015 -0.30 0} 5 0.5
do_wait time 0.2
set_anim [Actor 1] swordheadhith 8 0
do_wait time 1
+gametime factor 1

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
#sq_actor eyes 2 {c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }


+sq_pen set WalkPos Knocker1Pos
do_action walk WalkPos 0
do_wait time 0.8
do_action anim takeboxa 0
do_wait time 0.2

do_action anim takeboxb 0
link_obj [inv_get [Actor 1] 0]
#do_wait time 2
link_obj [inv_get [Actor 1] 0] [Actor 0] 1
do_wait time 0.5

+sq_pen set WalkPos 0
+sq_pen move WalkPos {-2 0 0}
do_action walk WalkPos 0
do_wait time 1.5
sq_actor express 0 good_awake
do_action rotate front 0
do_wait time 0.5


sq_actor actionlist 0 {{anim cheer} {rotate back} {anim cheer} {rotate front} {anim cheer} {anim warmbutt} {anim cheer} {anim breathe} {anim cheer} {rotate front}}
do_action anim breathe 0
set_anim [Actor 1] swordheadhith 8 1
do_action anim swordstillani 1
do_wait time 0.5
-if {[get_objgender [Actor 0]]=="male"} {sound play jubel_4o 1} else {sound play jubel_4x 1}
#-sound play jubel_4o 1
do_wait time 1

#Schock
do_action anim shock 1
-if {[get_objgender [Actor 0]]=="male"} {sound play jubel_4o 1} else {sound play jubel_4x 1}
#-sound play jubel_4o 1
do_wait time 0.6

sq_object delete 1
do_action rotate 1 0
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
#sq_actor eyes 2 {c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 { u u u u u u 9 u u u u u u u u 9 u u u u u u u u u u 9 u u u u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u 9 u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u 9 u u u u u u u u u u 9 u u u u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u 9 u u u u u u u u u u u u 9 u u u u u u u u u 9 u u u u u u u u u u u u u 9 u u u u u u u u u 9 u u}
#Knocker wütend; sagt: Glaub ja nicht, dass du gewonnen hast. Möge Fenris dich verschlingen! Wir sehen uns noch!
do_text 2160a 1 {talkc} Moege
do_wait time 5.1

#Dreht sich um Hochnäsig dreht er auf dem Absatz um, und lässt den Wiggle stehen.
#do_text "Ich drehe mich auf dem Absatz um und lasse den Wiggle stehen." 1
+sq_camera fix TribuenePos 1.1 -0.2 0
do_action rotate right 1
do_wait time 0.5
do_action walk KnockerRausPos 1
+start_fade 5 0
+option set showUI 0
do_wait time 4
#Knocker winkt seinen Kumpels und alle vier stampfen davon.
#do_text "Wir Knockers gehen." 1

+sq_object delete all
+sq_camera get
adaptive_sound changetheme tournament
#+cancel_fade

#do_text "4 Wiggles liegen sich in den Armen..." 0
#do_wait time 2
#do_text "Schnitt-nach Walhalla" 0
#do_wait time 3

#2160a Glaub ja nicht, dass du gewonnen hast. Möge Fenris dich verschlingen! Wir sehen uns noch!
#2160b Du, als Führer der Wiggles - sei auf der Hut! Es wird ein beschwerlicher Weg. Ein Weg voller Gefahren in die Unterwelt, ein Weg strotzend vor Gegnern und Tücken. Ein Weg...
#2160c Voller Feinde und voller Freunde, aber auch ein Weg voller Erfindungen und --
#2160d REICHT MENSCH!.. äh...Gott!... Das kriegt der doch eh nicht hin!
#2160e Deswegen sollst du ihn ja auch begleiten!
#2160f Hähä...hmmm, grrr...


