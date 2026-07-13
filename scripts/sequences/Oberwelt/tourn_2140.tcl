#CLIP 2140 - WETTKAMPF - WIGGLES IN TROPHÄEN-HÖHLE
#Trophäenhöhle, Knocker schnappt Goldhamster
#elf say "Sequenz tourn-2140"

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmotourn
#-----------------------------------------

+sq_text file Tournament
sq_audio open Clip_2140

sq_actor express 0 good_normal

#zum Testen Start
set_anim [obj_query [Actor 0] "-class Tuer_kaserne -range 10 -limit 1"] tuer_kaserne.oeffnen_b 0 1
#zum Testen Ende

+sq_pen set KameraStart 0
+sq_pen move KameraStart {1 -0.5 0}

+sq_camera fix KameraStart 0.9 -0.15 0.1 0.5
do_wait time 0.1

+sq_pen set GeneralPos [Getobjpos Dummy_Obw_goldhamster 0 30] ;#+owner 0
+sq_pen set Vollbild GeneralPos
+sq_pen move Vollbild {0 0 0}

+sq_pen set Knocker1Pos 0
+sq_pen move Knocker1Pos {-2.1 0 0.5}

+sq_pen set TrophyPos GeneralPos
+sq_pen move TrophyPos {-0.5 0 0.5}
+sq_pen set Trophy2Pos GeneralPos
+sq_pen move Trophy2Pos {-0.1 0 0.8}
+sq_pen set TrophyKameraPos TrophyPos
+sq_pen move TrophyKameraPos {4.0 0.4 -20}
+sq_pen set CheerPos GeneralPos
+sq_pen move CheerPos {-2.4 0 4.2}
sq_pen set CheerKickPos CheerPos
sq_pen move CheerKickPos {-0.3 0 0.5}

+sq_pen set KameraLinksNahPos GeneralPos
+sq_pen move KameraLinksNahPos {-2 0 3}
#FernPos mit Winkeln: Zoom 1.0 -0.57=y -0.359=x
+sq_pen set KameraLinksFernPos GeneralPos
+sq_pen move KameraLinksFernPos {0 0 3}

+sq_pen set Ladder1Pos GeneralPos
+sq_pen move Ladder1Pos {7 0 5}
+sq_pen set Ladder2Pos GeneralPos
+sq_pen move Ladder2Pos {7 -0 5}
+sq_pen set Ladder3Pos GeneralPos
+sq_pen move Ladder3Pos {7 -4.5 5}
+sq_pen set Ladder4Pos GeneralPos
+sq_pen move Ladder4Pos {3 0 5}
+sq_pen set KameraLadderPos GeneralPos
+sq_pen move KameraLadderPos {5 0 5}

+sq_pen set ElfPos GeneralPos
+sq_pen move ElfPos {5 0 10}
+sq_pen set ElfBeamPos GeneralPos
+sq_pen move ElfBeamPos {3 5.5 12}
+sq_pen set ElfTextPos GeneralPos
+sq_pen move ElfTextPos {1 -1 8}

+sq_camera move 0 0.9 -0.2 0 0.5

do_action walk CheerPos 0

#Object 0 - Der Knocker
sq_object summon Zwerg Knocker1Pos 2
#call_method [Object 0] Editor_Set_Info {{name Knicker} {gender male}}
call_method [Object 0] init
do_wait time 0.1
sq_actor find Zwerg 20 1 2
do_action run CheerPos 0
do_wait time 0.3
sq_camera move 0 1.0 -0.2 0 0.2
do_change muetze stone 1 auf noanim
do_wait time 0.25
do_action run CheerKickPos 1
do_wait time 1.0
sq_camera follow 0 1.2 -0.05 -0.3 0.1;#0.03
do_wait time 0.5
do_action walk CheerKickPos 1
do_wait time 2.5


sq_camera move TrophyKameraPos 0.65 -0.05 0.4 0.5
do_wait time 0.5
do_action anim cheer 0
do_wait time 0.5
do_action anim cheer 0
do_wait time 0.5

do_wait time 0.5
sq_wait 1
do_action run CheerKickPos 1
sq_wait none
#do_wait time 0.6
#do_action walk TrophyPos 0
+sq_camera fix KameraLinksNahPos 0.9 -0.57 -0.359
do_action rotate 0 1
do_wait time 0.4
do_action anim mann.treten_hintern 1
do_wait time 0.35

action [Actor 0] wait 0
set_anim [Actor 0] standbackhith 0 1
do_wait time 0.1
do_action run Trophy2Pos 1
do_wait time 1.2
#do_wait time 0.1
set_anim [Actor 0] standbackhith 14 0
do_wait time 1.2
do_action rotate Trophy2Pos 1
do_wait time 0.1
+del [Getobjref Dummy_Obw_goldhamster]
do_wait time 0.5
do_action anim takeboxa 1
do_wait time 0.1
sq_object summon Dummy_Obw_goldhamster TrophyPos
link_obj [Object 1] [Actor 1] 0
do_wait time 0.2

do_action run Ladder1Pos 1
do_wait time 1
do_action beam Ladder2Pos 1
do_wait time 0.4
sq_camera fix KameraLadderPos 1.1 -0.019 0.242
do_action run Ladder3Pos 1
do_elf path ElfBeamPos ElfPos
do_wait time 3

sq_camera move KameraLadderPos 1.2 -0.1 0.242
do_elf lookat 1
do_wait time 1
do_elf lookat 0
do_wait time 0.4
do_elf move TrophyPos
+sq_camera move KameraLinksFernPos 0.9 -0.47 -0.45
do_wait time 3.5
sq_actor actionlist 0 {{rotate right} {anim standloopa} {anim scratch}}
do_action anim standup 0

#ELFE text Hinterher! Worauf wartest du?!
elf unfollowview
do_elf text 2140a {auffordern} Hinterher
do_wait time 2.5
do_elf text 2140b {meckern} Wenn
do_wait time 5.2

do_action walk Ladder1Pos 0
do_wait time 1.5

+do_change muetze stone 1 ab noanim
+do_elf hide
+sq_object delete all
+sq_camera get

+adaptive_sound changetheme tournament

