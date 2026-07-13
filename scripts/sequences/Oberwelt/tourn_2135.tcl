#CLIP 2135 - WETTKAMPF - WIGGLES BEIM BIERBRAUEN
# WIGGLES BEIM BIERBRAUEN
#Kein Text

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmotourn
#-----------------------------------------

sq_pen set KnockerBrauerei [Getobjpos Brauerei 0 200 2]
sq_pen set Knocker1Pos KnockerBrauerei
sq_pen move Knocker1Pos {-1 0 3}
sq_pen set Knocker2Pos KnockerBrauerei
sq_pen move Knocker2Pos {1 0 5}
sq_pen set StuhlPos KnockerBrauerei
sq_pen move StuhlPos {3 0 1}
sq_pen set BarrelStartPos Knocker1Pos
sq_pen move BarrelStartPos {0.5 0 0}
sq_pen set BarrelEndePos StuhlPos
sq_pen move BarrelEndePos {-0.3 0 1}
sq_pen set BarrelStuhlPos StuhlPos
sq_pen move BarrelStuhlPos {0 -0.5 0.5}

sq_pen set KameraTuerPos KnockerBrauerei
sq_pen move KameraTuerPos {13 0 0}
sq_pen set Tuer1Pos KnockerBrauerei
sq_pen move Tuer1Pos {11.0 0 0}
sq_pen set Tuer2Pos KnockerBrauerei
sq_pen move Tuer2Pos {12.0 0 0}
sq_pen set RunStartPos KnockerBrauerei
sq_pen move RunStartPos {11.9 0 3.5}
sq_pen set RunEndePos KnockerBrauerei
sq_pen move RunEndePos {14.4 0 3.5}

sq_pen set KameraEndePos Tuer2Pos

sq_pen set WiggleBrauerei [Getobjpos Brauerei 0 200 0]

#sq_camera fix WiggleBrauerei 1.5 -0.2 0
sq_camera fix 0 1.5 -0.2 0
#sq_actor find Zwerg 40 2 0 WiggleBrauerei
#sq_actor find Zwerg 30 2 0 TriggerPos


#Fade out
+start_fade 1 0

sq_object summon Zwerg Knocker1Pos 2
call_method [Object 0] Editor_Set_Info {{name Knock} {gender male}}
call_method [Object 0] init
do_wait time 0.2

sq_object summon Zwerg Knocker2Pos 2
call_method [Object 1] Editor_Set_Info {{name Knuck} {gender male}}
call_method [Object 1] init
do_wait time 0.2

sq_actor find Zwerg 10 2 2 Knocker1Pos
do_wait time 0.6

#Object 2
sq_object summon Dummy_Holzstuhl StuhlPos
do_change muetze stone 1 auf noanim
do_change muetze stone 2 auf noanim
do_wait time 0.3

sq_wait none
sq_actor actionlist 2 {{anim leftright} {anim standloopa} {anim jumpa} {anim standloopc} {rotate right}}
do_action rotate back 2

#Die Knockers sind mit Brauen fertig, rollen ihr Fass auf eine Holzpalette.
sq_camera fix KnockerBrauerei 1.5 -0.2 0
do_action barrelwalk BarrelEndePos 1

#fade in
+start_fade 1 1

#do_text "Ich packe das Fass auf den Stuhl" 2

do_wait time 2.55
sq_camera selset inout
sq_camera move KameraTuerPos 1.1 -0.1 0.5 0.3
do_wait time 3
#do_action irgendeine raufpackgeste 2
sq_object summon Bier BarrelStuhlPos
set_physic [Object 3] 0
do_action run Tuer1Pos 2
do_action run Tuer2Pos 1
do_wait time 6.8

do_action rotate 2.5 1
do_wait time 0.5
do_action rotate 3.3 2
do_wait time 0.6

do_action anim scratchhead 1
do_action anim talkacnta 2

#Der Schalter wird gedrückt
do_action anim switchup 1
do_wait time 0.4
action [obj_query [Getobjref Zwerg 2] "-class Schalter_hebel_holz_up -range 10 -limit 1"] anim press {action this anim release}
#Die Tür geht auf
set_anim [obj_query [Getobjref Zwerg 2] "-class Tuer_kaserne -range 10 -limit 1"] tuer_kaserne.oeffnen_b 0 1
do_wait time 0.5
do_action rotate right 1
do_action rotate right 2

do_wait time 1.5
#Die Tür geht zu
set_anim [obj_query [Getobjref Zwerg 2] "-class Tuer_kaserne -range 10 -limit 1"] tuer_kaserne.kurz_b 5 1
do_wait time 2

do_action rotate 1 2
do_action rotate 2 1
do_wait time 0.4
do_action anim showright 1
do_wait time 0.4
do_action anim talkrepoa 2
do_wait time 0.6
do_action walk RunStartPos 2
sq_actor actionlist 1 {{anim talkc} {anim standloopb}}
do_action anim standloopa 1
do_wait time 3
do_action rotate 2 1
do_action rotate right 2
do_wait time 0.4
do_action anim talkm 2
do_wait time 0.2

#Zweites Mal drücken ... do_text "Ich drück den Knopf nochmal" 2
do_action rotate 2.5 1
do_wait time 0.5
do_action anim switchup 1
do_wait time 0.35
sq_actor actionlist 2 {{anim hungry} {rotate right} {anim rebound}}
#do_action anim rebound 2
do_action rotate left 2
do_wait time 0.01

action [obj_query [Getobjref Zwerg 2] "-class Schalter_hebel_holz_up -range 10 -limit 1"] anim press {action this anim release}
set_anim [obj_query [Getobjref Zwerg 2] "-class Tuer_kaserne -range 10 -limit 1"] tuer_kaserne.oeffnen_b 0 1
do_wait time 2.4
do_action rotate 0.7 1
do_wait time 0.75
set_anim [obj_query [Getobjref Zwerg 2] "-class Tuer_kaserne -range 10 -limit 1"] tuer_kaserne.kurz_b 5 1
do_wait time 2
sq_actor actionlist 1 {{anim bowllose} {anim bowllose} {anim bowllose} {anim bowllose}}
do_action anim bowllose 1
do_wait time 0.5

sq_wait none
sq_camera move KameraEndePos 0.65 -0.1 0.5 1.0
do_wait time 1
do_action beam Knocker1Pos 2
do_wait time 1

#Fade out
+start_fade 2 0

#sq_camera fix WiggleBrauerei 1.5 -0.2 0
sq_camera fix 0 1.5 -0.2 0
do_change muetze stone 1 ab noanim
do_change muetze stone 2 ab noanim
#fade in
+start_fade 2 1

+sq_object delete all
+do_elf hide
do_wait time 1
+adaptive_sound changetheme tournament


