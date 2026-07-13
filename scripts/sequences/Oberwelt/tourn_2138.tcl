#WIGGLES FERTIG MIT BRAUEN
#elf say "Sequenz tourn-2138"

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmotourn
#-----------------------------------------

sq_text file Tournament
sq_audio open Clip_2138

+sq_pen set VoodooBrauerei [Getobjpos Brauerei 0 200 1]
+sq_pen set KameraVoodooBrauerei VoodooBrauerei
+sq_pen move KameraVoodooBrauerei  {-1 0 0}
+sq_pen set VoodooZuschauerraum VoodooBrauerei ;#{Brauerei x y z=9}
+sq_pen set Voodoo1Pos VoodooBrauerei
+sq_pen move Voodoo1Pos {-1.5  0 2}
+sq_pen set Voodoo2Pos VoodooBrauerei
+sq_pen move Voodoo2Pos {-0.5 0 2}
+sq_pen set Voodoo3Pos VoodooBrauerei
+sq_pen move Voodoo3Pos { 0 0 0.5}
+sq_pen set BarrelEndePos Voodoo2Pos
+sq_pen move BarrelEndePos {5 0 0}
+sq_pen set BarrelWalkEndePos Voodoo2Pos
+sq_pen move BarrelWalkEndePos {5 0 0}
+sq_pen set BarrelAufStuhlPos BarrelEndePos
+sq_pen move BarrelAufStuhlPos {0 0 1}

+sq_pen set SchalterVoodooPos VoodooBrauerei
+sq_pen move SchalterVoodooPos {12 0 0}
+sq_pen set TuerVoodooPos VoodooBrauerei
+sq_pen move TuerVoodooPos {13 0 0}
+sq_pen set TuerVornVoodooPos VoodooBrauerei
+sq_pen move TuerVornVoodooPos {12 0 3.5}
+sq_pen set TuerMitteVoodooPos VoodooBrauerei
+sq_pen move TuerMitteVoodooPos {14.5 0 3.5}
+sq_pen set TuerHintenVoodooPos VoodooBrauerei
+sq_pen move TuerHintenVoodooPos {15.3 0 3.5}

#sq_pen set Bierpos [get_pos [obj_query this "-class Dummy_Holzstuhl_b -range 15 -limit 1"]]

+sq_pen set StuhlPos VoodooBrauerei
+sq_pen move StuhlPos {5 0 0}
+sq_pen set StuhlVoodooObenPos StuhlPos
+sq_pen move StuhlVoodooObenPos {0 -1 0}
+sq_pen set BigBarrelPos VoodooBrauerei
+sq_pen move BigBarrelPos {2.6 0.05 0.65}
+sq_pen set BierPartikelPos BigBarrelPos
+sq_pen move BierPartikelPos {0.35 -0.7 0.2};#0.2


+sq_pen set WiggleBrauerei [Getobjpos Brauerei 0 30]
#sq_camera fix WiggleBrauerei 1.5 -0.2 0
+sq_pen set WiggleStuhlPos WiggleBrauerei
+sq_pen move WiggleStuhlPos {5 0 0}
+sq_pen set WiggleFass1Pos WiggleBrauerei
+sq_pen move WiggleFass1Pos {-2 0 1}
+sq_pen set WiggleFass2Pos WiggleBrauerei
+sq_pen move WiggleFass2Pos {2 0 2}
+sq_pen set WiggleFass3Pos WiggleBrauerei
+sq_pen move WiggleFass3Pos {1 0 1}
+sq_pen set WiggleFass4Pos WiggleBrauerei
+sq_pen move WiggleFass4Pos {-1 0 2}

#1. Elfe sagt zu Wiggles fertig ...
+sq_pen set ElfPos WiggleBrauerei
+sq_pen move ElfPos {-1 -1 6}
+sq_pen set ElfBeamPos WiggleBrauerei
+sq_pen move ElfBeamPos {12 -6 6}

+sq_camera fix WiggleBrauerei 1.4 -0.2 0

sq_actor find Zwerg 15 1 0 WiggleBrauerei
do_wait time 1

sq_wait none
do_action walk WiggleFass1Pos 0

#2. VOODOOZWERGE - BRAUEREI
#Voodoozwerg erzeugen
#Object 0
sq_object summon Zwerg Voodoo1Pos 1
call_method [Object 0] Editor_Set_Info {{name Voodoo} {gender male}}
call_method [Object 0] init
do_wait time 0.2
#Object 1
sq_object summon Zwerg Voodoo2Pos 1
call_method [Object 1] Editor_Set_Info {{name Uoodooi} {gender female}}
call_method [Object 1] init
do_wait time 0.2

#Object 2
sq_object summon Zwerg Voodoo3Pos 1
call_method [Object 2] Editor_Set_Info {{name Doodoo} {gender male}}
call_method [Object 2] init
do_wait time 0.2

do_action rotate 1 0
do_action walk WiggleFass2Pos 1

#sq_actor find Zwerg 20 1 VoodooBrauerei

#Voodoos finden
sq_actor find Zwerg 20 3 1 Voodoo1Pos
do_wait time 1.8
do_change muetze sparetime 4
do_change muetze sparetime 3
do_change muetze sparetime 2
do_wait time 0.2

#Object 6
sq_object summon Dummy_Holzstuhl_a StuhlPos
do_wait time 0.1
do_action rotate 1 0
do_action rotate 0 1

#fade out
start_fade 1 0
do_action walk WiggleFass1Pos 0
do_action walk WiggleFass2Pos 1
do_wait time 1

#Einer der Vodoozwerge rollt das Fass auf die Palette.
#do_text "Ich kicke das Fass aus dem Bildschirm" 2
do_action barrelwalk BarrelWalkEndePos 2
sq_object summon Bier BarrelEndePos
set_physic [Object 3] 0
do_action run SchalterVoodooPos 3
sq_actor actionlist 4 {{anim drinktubstart} {anim drinktubloop} {anim drinktubstop}}
do_action rotate back 4
sq_camera fix KameraVoodooBrauerei 1.0 -0.25 -0.1
#fade in
+start_fade 2 1
do_wait time 1
do_action drunk Voodoo1Pos 4
do_wait time 0.5
do_action drunk Voodoo3Pos 4
do_wait time 1.5

do_wait time 1.5
sq_actor actionlist 4 {{anim drinktubstart} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubloop} {anim drinktubstop}}
do_action rotate back 4
sq_object beam [Object 3] StuhlVoodooObenPos
do_action beam SchalterVoodooPos 3
do_action beam TuerVornVoodooPos 2
do_wait time 0.5
sq_camera fix TuerVoodooPos 1.0 -0.05 0.5

do_action rotate 3 2
do_action rotate back 3
do_wait time 0.4
do_action anim switchup 3
do_wait time 0.4
do_action rotate TuerHintenVoodooPos 3
action [obj_query [Getobjref Zwerg 3] "-class Schalter_hebel_holz_up -range 10 -limit 1"] anim press {action this anim release}
#Die Tür geht auf
set_anim [obj_query [Getobjref Zwerg 3] "-class Tuer_kaserne -range 10 -limit 1"] openb 0 1
do_wait time 0.5
#do_action run TuerMitteVoodooPos 2
+sq_pen set ForwardPos 2
+sq_pen move ForwardPos {2 0 0}
do_action run ForwardPos 2
#do_action run BigBarrelPos 3
do_wait time 1.5
sq_camera move 2 0.8 -0.05 0.6 0.7
sq_actor eyes 2 {3 3 9 3 3 3 9 3 3 3 9 3 3 3 9 3 3 3 9 3 3 9 3 3 3 3 9 3 3 3 3 3 9 3 3 3 3 3 3 9 3 3 9 3 3 3 3 3 3 3 3 9 3 3 3 3 3 3 3 9 3 3 9 3 3 3 3 3 3 3 3 9 3 3 3 3 3 3 3 9 3 3 9 3 3 3 3 9 3 3 3 3 9 3 3 3 3 9 3 3 3 9 3 3 9 3 3 3 3 9 3 3 3 3 9 3 3 3 3 3 3 3 9 3 3 9 3 3 3 3 3 3 3 3 9 c c c c c c c 9 c c 9 c c c c c c c c 9 c c c c c 9}
sq_actor mouth 3 {14}
sq_actor eyes 3 { 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5}
do_action walk TuerHintenVoodooPos 2
do_wait time 1
do_action anim scratchhead 2

do_wait time 0.7
do_action beam BigBarrelPos 3
sq_actor actionlist 3 {{anim drinkbarrelloop} {anim drinkbarrelloop} {anim drinkbarrelloop} {anim drinkbarrelloop} {anim drinkbarrelloop} {anim drinkbarrelloop} {anim drinkbarrelloop} {anim drinkbarrelloop} {anim drinkbarrelloop}}

do_action anim drinkbarrelstart 3

#gametime factor 0.2
do_action rotate BigBarrelPos 2
do_wait time 0.3
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
#+gametime factor 1
sq_camera fix 3 0.7 -0.1 -0.7
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_action skipp StuhlPos 2
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.04 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.045 0} 20 1
do_wait time 0.1
do_particle create 15 BierPartikelPos { 0 0.05 0} 20 1
do_wait time 0.3

sq_camera fix TuerVoodooPos 1.0 -0.05 0.5

set_anim [obj_query [Getobjref Zwerg 2] "-class Tuer_kaserne -range 10 -limit 1"] closeb 0 1

#fade out

#Übergang zu:

#3. KNOCKERS - KNOPF-RÄTSEL
# SCHNITT AUF:
#fade out
sq_pen set KnockerBrauerei [Getobjpos Brauerei 0 240 2]
#sq_pen set KnockerTuer KnockerBrauerei
#sq_pen move KnockerTuer {10 0 0}
#sq_pen set Knocker1Pos KnockerTuer
#sq_pen move Knocker1Pos {-1 0 0}
#sq_pen set Knocker2Pos KnockerTuer
#sq_pen move Knocker2Pos {-2 0 0}

+sq_pen set KameraTuerPos KnockerBrauerei
+sq_pen move KameraTuerPos {13 0 0}
+sq_pen set Tuer1Pos KnockerBrauerei
+sq_pen move Tuer1Pos {12 0 0}
+sq_pen set Tuer2Pos KnockerBrauerei
+sq_pen move Tuer2Pos {11 0 0}
+sq_pen set KameraSchalterPos Tuer2Pos
+sq_pen move KameraSchalterPos {4 0.6 -30}

+sq_pen set RunStartPos KnockerBrauerei
+sq_pen move RunStartPos {11.9 0 3.5}
+sq_pen set RunEndePos KnockerBrauerei
+sq_pen move RunEndePos {14.4 0 3.5}
+sq_pen set KameraBummsPos RunEndePos
+sq_pen move KameraBummsPos { 0 0 0 }

+sq_pen set KameraEndePos Tuer2Pos

#Object 7
sq_object summon Zwerg RunStartPos 2
call_method [Object 5] Editor_Set_Info {{name Knock} {gender male}}
call_method [Object 5] init
do_wait time 0.2

#Object 8
sq_object summon Zwerg Tuer1Pos 2
call_method [Object 6] Editor_Set_Info {{name Knuck} {gender male}}
call_method [Object 6] init
do_wait time 0.2

sq_actor find Zwerg 40 2 2 Tuer1Pos
do_wait time 0.7
do_change muetze stone 5 auf
do_change muetze stone 6 auf
do_wait time 0.3

do_action rotate back 5
do_action rotate right 6
do_wait time 0.4
sq_camera fix KameraTuerPos 1.1 -0.1 0.5 0.3
action [obj_query [Getobjref Zwerg 5] "-class Schalter_hebel_holz_up -range 10 -limit 1"] anim press {action this anim release}
do_action anim switchup 5
do_wait time 0.35
#fade in
do_action anim rebound 6
do_wait time 0.01
set_anim [obj_query [Getobjref Zwerg 5] "-class Tuer_kaserne -range 10 -limit 1"] tuer_kaserne.oeffnen_b 0 1
do_wait time 0.4
do_action rotate 0.7 5
do_wait time 0.7
set_anim [obj_query [Getobjref Zwerg 5] "-class Tuer_kaserne -range 10 -limit 1"] tuer_kaserne.kurz_b 5 1
sq_actor actionlist 5 {{anim standloopa} {anim bowllose} {anim talkacnga}}
do_action rotate RunEndePos 5

start_fade 3 0
do_wait time 3.5
do_action rotate 5 6
do_wait time 0.5

sq_wait none


+sq_camera fix WiggleBrauerei 1.4 -0.2 0

#fade in zu Wiggles
+start_fade 1 1
do_wait time 1


#3. Elfe sagt Bescheid alles fertig.
##################################################
sq_wait elf
do_elf beam ElfBeamPos

sq_wait none
do_action walk WiggleFass3Pos 0
do_action walk WiggleFass4Pos 1

sq_wait elf
do_elf move ElfPos

sq_wait none
#2138a Reicht!
do_elf text 2138a {reden_a} Reicht
do_action rotate front 0
do_action rotate front 1
do_wait time 1

#2138a Reicht!
do_elf text 2138b {zeigen_rechts} Schnell
do_wait time 3.2

+sq_object delete all
+do_elf hide


+adaptive_sound changetheme tournament

