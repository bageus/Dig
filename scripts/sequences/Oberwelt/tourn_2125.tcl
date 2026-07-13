# WIGGLES KOMMEN ZU PILZEN
#---inserted-by-Jan---MUSIC---------------
adaptive_sound markerdisable
adaptive_sound changethemenow atmotourn
#-----------------------------------------

sq_text file Tournament
sq_audio open Clip_2125

+sq_pen set VoodooBrauerei [Getobjpos Brauerei 0 400 1]
+sq_pen set VoodooZuschauerraum VoodooBrauerei ;#{Brauerei x y z=9}
+sq_pen move VoodooZuschauerraum {-30 0 0}
+sq_pen set Voodoo1Pos VoodooZuschauerraum
+sq_pen move Voodoo1Pos {-2 0 4}
+sq_pen setz Voodoo1Pos 13
+sq_pen set Voodoo2Pos Voodoo1Pos
+sq_pen move Voodoo2Pos {-3 0 0}
+sq_pen setz Voodoo2Pos 13
+sq_pen set Voodoorenn1 Voodoo1Pos
+sq_pen move Voodoorenn1 {15 0 0}
+sq_pen setz Voodoorenn1 13
+sq_pen set Voodoorenn2 Voodoo1Pos
+sq_pen move Voodoorenn2 {17 0 0}
+sq_pen setz Voodoorenn2 13

+sq_pen set Voodoorenn3 Voodoorenn1
+sq_pen move Voodoorenn3 {10 0 0}
+sq_pen setz Voodoorenn3 13
+sq_pen set Voodoorenn4 Voodoorenn2
+sq_pen move Voodoorenn4 {10 0 0}
+sq_pen setz Voodoorenn4 13

+sq_pen set Voodoorenn5 Voodoorenn3
+sq_pen move Voodoorenn5 {5 0 0}
+sq_pen setz Voodoorenn5 13
+sq_pen set Voodoorenn6 Voodoorenn4
+sq_pen move Voodoorenn6 {5 0 0}
+sq_pen setz Voodoorenn6 13

+sq_pen set Nirvana [Getobjpos Brauerei 0 400 2]

#Zuschauer
+sq_pen set Zuschauer0Pos VoodooZuschauerraum
+sq_pen move Zuschauer0Pos {0 0 0.2}
+sq_pen set Zuschauer1Pos Zuschauer0Pos
+sq_pen move Zuschauer1Pos {0.7 0 0.2}
+sq_pen set Zuschauer2Pos Zuschauer1Pos
+sq_pen move Zuschauer2Pos {0.8 0 0}
+sq_pen set Zuschauer3Pos Zuschauer1Pos
+sq_pen move Zuschauer3Pos {6 0 0}
+sq_pen set Zuschauer4Pos Zuschauer1Pos
+sq_pen move Zuschauer4Pos {7 0 0}
+sq_pen set Zuschauer5Pos Zuschauer1Pos
+sq_pen move Zuschauer5Pos {8 0 0}
+sq_pen set Zuschauer6Pos Zuschauer1Pos
+sq_pen move Zuschauer6Pos {9 0 0}
+sq_pen set Zuschauer7Pos Zuschauer1Pos
+sq_pen move Zuschauer7Pos {10 0 0}
+sq_pen set Zuschauer8Pos Zuschauer1Pos
+sq_pen move Zuschauer8Pos {10.5 0 0}
+sq_pen set Zuschauer9Pos Zuschauer1Pos
+sq_pen move Zuschauer9Pos {7.5 0 -0.5}
+sq_pen set Zuschauer10Pos Zuschauer1Pos
+sq_pen move Zuschauer10Pos {9.2 0 -0.5}
+sq_pen set Zuschauer11Pos Zuschauer1Pos
+sq_pen move Zuschauer11Pos {9.8 0 -1}
+sq_pen set Zuschauer12Pos Zuschauer1Pos
+sq_pen move Zuschauer12Pos {7.4 0 0.5}
+sq_pen set Zuschauer13Pos Zuschauer1Pos
+sq_pen move Zuschauer13Pos {8.7 0 0.3}

+sq_pen set FeuerstellePos [Getobjpos Feuerstelle 0 100]

+sq_pen set Feuer1Pos FeuerstellePos
+sq_pen move Feuer1Pos {-1 0 0}
+sq_pen set Feuer2Pos FeuerstellePos
+sq_pen move Feuer2Pos { 1 0 0}

sq_actor find Zwerg 1000 3 0
+sq_pen set Wiggle1Pos FeuerstellePos
+sq_pen move Wiggle1Pos {3 0 -3}
+sq_pen set Wiggle2Pos FeuerstellePos
+sq_pen move Wiggle2Pos {-3 0 -3}

+sq_pen set ElfPos FeuerstellePos
+sq_pen move ElfPos {1 -0.4 0}
+sq_pen set ElfStartPos ElfPos
+sq_pen move ElfStartPos {-10 -3 2}

+sq_pen set KameraStartPos FeuerstellePos
+sq_pen move KameraStartPos {0 0 0}
+sq_pen set KameraVorEndePos KameraStartPos
+sq_pen move KameraVorEndePos {38 0 0}
#Zwerg geht zur Feuerstelle

sq_wait none
sq_camera selset inout
sq_camera move KameraStartPos 1.2 -0.2 0 0.2

do_action walk Feuer2Pos 0
do_action walk Feuer1Pos 1
do_wait time 2.5

sq_camera selset standard
sq_camera fix KameraVorEndePos 1.2 -0.2 0.5 0.3

#nebenbei wird Voodooszene aufgebaut
#Voodoo laufen zur Brauerei Zwischensequenz
##################################################################
#Voodoozwerg erzeugen
+sq_object summon Zwerg Voodoo1Pos 1
+call_method [Object 0] Editor_Set_Info {{name Voodoo1} {gender female}}
+call_method [Object 0] init
do_wait time 0.2

+sq_object summon Zwerg Voodoo2Pos 1
+call_method [Object 1] Editor_Set_Info {{name Voodoo2} {gender male}}
+call_method [Object 1] init
do_wait time 0.2
sq_actor find Zwerg 500 2 1
do_wait time 0.6

#Object 2
sq_object summon Holzkiepe
link_obj [Object 2] [Actor 4] 5
do_wait time 0.1

#Object 3
sq_object summon Holzkiepe
link_obj [Object 3] [Actor 5] 5
do_wait time 0.1


#Die Zuschauer:
#Object 4
sq_object summon Zwerg Zuschauer1Pos 1
call_method [Object 4] init
do_wait time 0.1

#Object 5
sq_object summon Zwerg Zuschauer2Pos 1
call_method [Object 5] init
do_wait time 0.1

#Object 6
sq_object summon Zwerg Zuschauer3Pos 1
call_method [Object 6] init
do_wait time 0.1

#Object 7
sq_object summon Zwerg Zuschauer4Pos 1
call_method [Object 7] init
do_wait time 0.1

#Object 8
sq_object summon Zwerg Zuschauer5Pos 1
call_method [Object 8] init
do_wait time 0.1

#Object 9
sq_object summon Zwerg Zuschauer6Pos 1
call_method [Object 9] init
do_wait time 0.1

#Object 10
sq_object summon Zwerg Zuschauer7Pos 1
call_method [Object 10] init
do_wait time 0.1

#Ausfaden

sq_camera selset inout
sq_camera move KameraStartPos 1.2 -0.2 0 0.2
sq_actor find Zwerg 40 10 any Zuschauer0Pos
do_change muetze sparetime 4 auf noanim
do_change muetze sparetime 5 auf noanim
do_wait time 5.1

#fade out hier
start_fade 1 0
#dann fade in in die Voodooszene

sq_wait none

sq_actor actionlist 6 {{anim applaud} {anim jumpa} {anim jumpb} {anim applaud} {anim cheer} {anim showup} {anim applaud} {anim applaud} {anim applaud} {anim jumpa} {anim applaud} {anim cheer} {anim applaud}}
sq_actor actionlist 7 {{anim cheer} {anim bowlwin} {anim jumpa} {anim applaud} {anim jumpa} {anim jumpa} {anim applaud} {anim applaud} {anim smokepot} {anim applaud} {anim jumpb} {anim applaud} {anim applaud}}
sq_actor actionlist 8 {{anim applaud} {anim applaud} {rotate right} {anim talkc} {anim showup} {rotate front} {anim applaud} {anim jumpa} {anim stretch} {anim cheer} {anim applaud} {anim jumpb} {anim applaud}}
sq_actor actionlist 9 {{anim smokepot} {anim smokepot} {anim smokepot} {anim smokepot} {anim smokepot} {anim smokepot} {anim applaud} {anim applaud} {anim cheer} {anim applaud} {anim jumpa} {anim cheer} {anim applaud}}
sq_actor actionlist 10 {{anim applaud} {anim cheer} {anim applaud} {anim applaud} {anim jumpfire} {anim applaud} {anim applaud} {anim applaud} {anim applaud} {anim applaud} {anim applaud} {anim applaud} {anim applaud}}
sq_actor actionlist 11 {{anim applaud} {anim applaud} {anim applaud} {rotate right} {anim cheer} {anim showup} {rotate front} {anim jumpfire} {anim applaud} {anim cheer} {anim applaud} {anim jumpfire} {anim applaud}}
sq_actor actionlist 12 {{anim applaud} {anim applaud} {anim applaud} {anim jumpb} {anim impatient} {anim applaud} {anim smokepipestart} {anim smokepipeloop} {anim smokepipeloop} {anim smokepipestop} {anim applaud} {anim applaud} {anim applaud}}

do_action anim applaud 6
do_action anim jumpa 7
do_action anim cheer 8
do_action anim cheer 9
do_action anim cheer 10
do_action anim cheer 11
do_action anim cheer 12

sq_camera selset standard
sq_camera fix Voodoo1Pos 1.3 -0.15 0 0.4
+start_fade 1 1
do_action run Voodoorenn1 4
do_action run Voodoorenn2 5
do_wait time 2
sq_camera move VoodooBrauerei 1.1 -0.4 -0.5 0.045
do_wait time 6.5
do_action run Voodoorenn3 4
do_action run Voodoorenn4 5
sq_camera move VoodooBrauerei 1.5 -0.4 0.5 0.055
do_wait time 4.5
do_action run Voodoorenn5 4
do_action run Voodoorenn6 5
do_wait 4
sq_camera move VoodooBrauerei 1.5 -0.4 0.5 0.07
do_wait time 1
sq_camera move VoodooBrauerei 1.7 -0.4 0.6 0.1
do_wait time 3.5

#fade out
start_fade 2 0
do_wait time 2
do_elf beam ElfStartPos

#fade in
+sq_camera fix FeuerstellePos 1.6 -0.2 0 0.3
do_wait camera
+start_fade 1 1
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
#
#Zurück zu den Wiggles
##################################################################

sq_wait none
sq_actor actionlist { 0 1 } {}
sq_wait { 1 0 }
do_action run Wiggle1Pos 0
do_action run Wiggle2Pos 1
+sq_camera move FeuerstellePos 1.4 -0.2 0 0.2
do_action rotate ElfPos 0
do_action rotate ElfPos 1
do_elf move ElfPos
do_wait time 0.5
do_action anim scratchhead 0
do_action anim leftright 1

catch {del [ref_get [Actor 4] myhairs]}
catch {del [ref_get [Actor 5] myhairs]}
do_wait time 1.2

#Elfe text Los mach! Bring die Pilze zur Brauerei - dahinten! Diese Voodoo-Heinis sind schon fertig!
do_elf text 2125a {zeigen_rechts} Los
do_wait time 7
#Elfe text
+do_elf hide
do_wait time 2

+do_action beam Wiggle1Pos 0
+do_action beam Wiggle2Pos 1
do_change muetze sparetime 5 ab noanim
do_change muetze sparetime 4 ab noanim
+sq_object delete all


#2125a Los mach! Bring die Pilze zur Brauerei - dahinten! Diese Voodoo-Heinis sind schon fertig!

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound markerenable
#-----------------------------------------

