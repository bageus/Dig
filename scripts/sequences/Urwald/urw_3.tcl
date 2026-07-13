#CLIP 3 - ELFE BIETET WIDERWILL9HILFE AN - START DES GAMES (OBERWELT)
sq_text file Urwald
sq_audio open Clip_3
+cancel_fade
+option set showUI 1
do_wait time 4
+sq_pen set Lagerplatz TriggerPos
+sq_pen move Lagerplatz {-3 0 -2}
+sq_camera fix Lagerplatz 1.35 -0.25 0 0.4

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmooberwelt
#-----------------------------------------

+sq_pen set WiggleStart1Pos Lagerplatz
+sq_pen move WiggleStart1Pos {-10 0 4}
+sq_pen set WiggleStart2Pos WiggleStart1Pos
+sq_pen move WiggleStart2Pos {1 0 0}
+sq_pen set WiggleStart3Pos WiggleStart1Pos
+sq_pen move WiggleStart3Pos {2 0 0}
+sq_pen set WiggleStart4Pos WiggleStart1Pos
+sq_pen move WiggleStart4Pos {3 0 0}
+sq_pen set WiggleStart5Pos WiggleStart1Pos
+sq_pen move WiggleStart5Pos {4 0 0}

+sq_pen set WiggleMitte1Pos Lagerplatz
+sq_pen move WiggleMitte1Pos {-2 0 4}
+sq_pen set WiggleMitte2Pos WiggleMitte1Pos
+sq_pen move WiggleMitte2Pos {1 0 0}
+sq_pen set WiggleMitte3Pos WiggleMitte1Pos
+sq_pen move WiggleMitte3Pos {2 0 0}
+sq_pen set WiggleMitte4Pos WiggleMitte1Pos
+sq_pen move WiggleMitte4Pos {3 0 0}
+sq_pen set WiggleMitte5Pos WiggleMitte1Pos
+sq_pen move WiggleMitte5Pos {4 0 0}

+sq_pen set WiggleEnde1Pos Lagerplatz
+sq_pen move WiggleEnde1Pos {-1 0 -1}
+sq_pen set WiggleEnde2Pos Lagerplatz
+sq_pen move WiggleEnde2Pos {-0.2 0 3}
+sq_pen set WiggleEnde3Pos Lagerplatz
+sq_pen move WiggleEnde3Pos {-2 0 -1.5}
+sq_pen set WiggleEnde4Pos Lagerplatz
+sq_pen move WiggleEnde4Pos {1.9 0 4}
+sq_pen set WiggleEnde5Pos Lagerplatz
+sq_pen move WiggleEnde5Pos {1.5 0 8}

+sq_pen set WiggleSchacht1Pos Lagerplatz
+sq_pen move WiggleSchacht1Pos {9.5 0 3.5}
+sq_pen set WiggleSchacht2Pos Lagerplatz
+sq_pen move WiggleSchacht2Pos {9.5 0 8}
+sq_pen set KameraSchachtPos Lagerplatz
+sq_pen move KameraSchachtPos {9 0 5}

+sq_pen set Elf1Pos Lagerplatz
+sq_pen move Elf1Pos {5 -1.5 9}
+sq_pen set ElfInvis1Pos Elf1Pos
+sq_pen move ElfInvis1Pos {-50 0 10}
+sq_pen set ElfInvis2Pos Elf1Pos
+sq_pen move ElfInvis2Pos {0 20 10}

+sq_pen set WiggleElfLook1Pos Elf1Pos
+sq_pen move WiggleElfLook1Pos { -0.5 0 -6}
+sq_pen set WiggleElfLook2Pos Elf1Pos
+sq_pen move WiggleElfLook2Pos {2 0 -4}
+sq_pen set WiggleElfLook3Pos Elf1Pos
+sq_pen move WiggleElfLook3Pos {1.3 0 -5.5}
+sq_pen set WiggleElfLook4Pos Elf1Pos
+sq_pen move WiggleElfLook4Pos {1.2 0 -3}
+sq_pen set WiggleElfLook5Pos Elf1Pos
+sq_pen move WiggleElfLook5Pos {2.9 0 -4}

+sq_pen set Ring1Pos Lagerplatz
+sq_pen move Ring1Pos {-1.9 -0.5 2}
+sq_pen set Ring2Pos Lagerplatz
+sq_pen move Ring2Pos {-1.0 -0.5 2}

+sq_pen set DropPos Lagerplatz
+sq_pen move DropPos {0.5 -0.5 1}

+sq_pen set WiggleKicher1Pos DropPos
+sq_pen move WiggleKicher1Pos {2 0.5 0}
+sq_pen set WiggleKicher2Pos DropPos
+sq_pen move WiggleKicher2Pos {0 0.5 -3.4}
+sq_pen set WiggleKicher3Pos DropPos
+sq_pen move WiggleKicher3Pos {-0.5 0.5 -3.4}
+sq_pen set WiggleKicher4Pos DropPos
+sq_pen move WiggleKicher4Pos {1 0.5 4}
+sq_pen set WiggleKicher5Pos DropPos
+sq_pen move WiggleKicher5Pos {-2 0.5 2}

+sq_pen set KameraEndePos Lagerplatz
+sq_pen move KameraEndePos {3 0 0}

+sq_pen set ElfRausPos Lagerplatz
+sq_pen move ElfRausPos {-14 0 0}

+sq_camera fix Lagerplatz 1.35 -0.25 0 0.4
+do_wait camera

+sq_actor find Zwerg 100 5
#+start_fade 1 1
do_wait time 2

##########################################################################################
#Das Beamen ist hier nur zum Testen nötig im Spiel sollten die Wiggles automatisch dort sein
do_action beam WiggleStart1Pos 0
do_action beam WiggleStart2Pos 1
do_action beam WiggleStart3Pos 2
do_action beam WiggleStart4Pos 3
do_action beam WiggleStart5Pos 4
do_wait time 1
do_change muetze sparetime 0
do_change muetze sparetime 1
do_change muetze sparetime 2
do_change muetze sparetime 3
do_change muetze sparetime 4
do_wait time 1

############################################################################################

#Ein WIGGLE hat ne Kiste in der Hand.
+sq_object summon Halbzeug_kiste 0
+call_method [Object 0] change_look tragen
do_wait time 0.2
+link_obj [Object 0] [Actor 0] 0
do_wait time 0.3


sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c }

#1. OBERWELT
#Das gewohnte Szenario nach dem "Neues Spiel".
#Die START-WIGGLES kommen ins Bild gelaufen...
do_action transport WiggleMitte1Pos 0
do_action walk WiggleMitte2Pos 1
do_action walk WiggleMitte3Pos 2
do_action walk WiggleMitte4Pos 3
do_action walk WiggleMitte5Pos 4
do_wait time 5

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c }
do_action walk WiggleMitte2Pos 1
do_action walk WiggleMitte3Pos 2
do_action walk WiggleMitte4Pos 3
do_action walk WiggleMitte5Pos 4
#do_elf move ElfInvis1Pos
do_wait time 6

sq_actor actionlist 1 {{rotate front} {anim scout}}
sq_actor actionlist 2 {{rotate back} {anim scout}}
sq_actor actionlist 3 {{anim standloopb} {anim lookup}}
sq_actor actionlist 4 {{anim jumpa} {anim scout}}
do_action anim breathe 1
do_action anim breathe 2
do_action anim breathe 3
do_action anim breathe 4
do_wait time 3.5

do_action transport WiggleEnde1Pos 0
do_action walk WiggleEnde2Pos 1
do_action walk WiggleEnde3Pos 2
do_action walk WiggleSchacht1Pos 3
do_action walk WiggleEnde5Pos 4
do_wait time 2.5

#ZWEI WIGGLE rennen vor zum Schacht und sehen ihn sich an.
sq_camera selset inout
sq_camera move KameraSchachtPos 1.1 -0.2 -0.4 1.2
sq_actor eyes 3 {c c c c 9 c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c 9 c c c c c c 9 c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c 9 c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c 9 c c c c c 9 c c c c 9 c c c c c 9 c c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c 9 c c c c 9 c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c 9 c c c c c c c 9 c c c c c c c c 9 c c c c c 9 c c c c c 9 c c c c c 9 c c c c c 9 c c c c c c 9 c c c c 9 c c c c c c }
do_action rotate front 0
do_action rotate front 1
do_action rotate front 2
do_action run WiggleSchacht1Pos 3
do_action run WiggleSchacht2Pos 4
#+sq_object summon Hamster Hamster1Pos
do_wait time 3.3
do_wait camera
#+call_method [Object 1] walk_pos Hamster2Pos
do_wait time 1.7
+sq_camera fix KameraSchachtPos 1.1 -0.2 -0.4
#Die Wiggles schauen runter in den Schacht
-if {[get_objgender [Actor 3]]=="male"} {sound play schacht_kucken_1o 0.8} else {sound play schacht_kucken_1x 0.8}
do_action anim lookdownstart 3
-if {[get_objgender [Actor 4]]=="male"} {sound play schacht_kucken_1o 0.8} else {sound play schacht_kucken_1x 0.8}
do_action anim lookdownstart 4
do_wait time 0.3
do_action anim lookdownloop 3
do_action anim lookdownloop 4
#+sq_object delete 1
do_wait time 2
do_action anim lookdownstop 4
do_wait time 0.3
do_action rotate 3 4
#WIGGLE mit Kiste stellt die Kiste ab.
+link_obj [Object 0]
+sq_object delete 0
do_wait time 0.1
+sq_object summon Feuerstelle DropPos
+call_method [Object 0] packtobox
do_wait time 0.7

#WIGGLE #1 #Ah! Hier muss es sein, wo Fenris ausgerissen ist.
+set_collision [Object 0] 1
sq_actor eyes 3 {c c c c 9 c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c 9 c c c c c c 9 c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c 9 c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c 9 c c c c c 9 c c c c 9 c c c c c 9 c c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c 9 c c c c 9 c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c 9 c c c c c c c 9 c c c c c c c c 9 c c c c c 9 c c c c c 9 c c c c c 9 c c c c c 9 c c c c c c 9 c c c c 9 c c c c c c }
do_action anim lookdownstop 4
do_text 003a 4 PosAc Ahh
do_wait time 2.5
do_action rotate 4 3
do_wait time 1.6

#Wiggle #2 FREUDEN-SPRUNG.
#WIGGLE #2 #Na endlich! Lass uns hier graben!
sq_actor actionlist 4 {{anim talkrentb}}
do_action anim talkrepoa 4
do_text 003b 3 {{jumpa} {talkacpoc}} Endlich
do_wait time 3.2

#WIGGLE #1 Joo!
do_text 003c 4 talkacpob Jooh
do_tooltakeout Spitzhacke 3
do_elf beam ElfInvis2Pos
do_wait time 1.5

#Sie zücken Grabwerkzeug, aber da kommt ---
do_tooltakeout Spitzhacke 4

do_elf move Elf1Pos
do_wait time 1

sq_camera move Elf1Pos 1.3 -0.25 0 0.5
#Elfe kommt angeflogen. Die beiden Wiggles am Schacht gehen zurück zu den anderen -
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
do_action walk WiggleElfLook1Pos 0
do_action walk WiggleElfLook2Pos 1
do_action walk WiggleElfLook3Pos 2
do_action walk WiggleElfLook5Pos 4
sq_actor actionlist 3 {{anim hackqustart} {anim hackquloop} {anim hackquloop} {anim hackquloop} {anim hackquend}}
do_action rotate back 3
do_elf lookat
do_wait time 3.5

do_action rotate Elf1Pos 0
do_action rotate Elf1Pos 1
do_action rotate Elf1Pos 2
do_action walk WiggleElfLook4Pos 3
do_action rotate Elf1Pos 4
#ELFE #(schroff und patzig) Ich soll auf euch aufpassen. Hmmm.-.. Eigentlich hab ich ja echt Besseres zu tun, als hier den Babysitter zu spielen!
elf unfollowview
do_elf text 003d {reden_b|reden_a|reden_b|reden_a} Ich
do_wait time 1

do_toolputaway 4
do_wait time 1
do_action rotate Elf1Pos 3
sq_actor actionlist 0 {{anim scratchhead} {anim standloopc} {anim standloopa}}
sq_actor actionlist 1 {{anim standloopa} {anim butterflya} {anim wait}}
sq_actor actionlist 2 {{anim sitdown} {anim standloopb} {anim standup}}
do_action anim standloopa 0
do_action anim stretch 1
do_action anim standloopb 2
do_elf lookat
do_wait time 1.5

do_toolputaway 3
do_wait time 2

#Elfe fliegt zu einem der Wiggles.
+sq_pen set Elf1Pos 4
+sq_pen move Elf1Pos {-1 -1 0}

+sq_pen set RingKameraPos 1
+sq_pen move RingKameraPos {-2 0 0}
sq_camera selset inout
sq_camera move RingKameraPos 1.3 -0.25 0.1 0.3
do_wait time 0.8

+sq_pen set Elf1Pos 2
+sq_pen move Elf1Pos {-2.5 -1 0}
do_elf move Elf1Pos

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c}
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c}
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c}
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c}

do_action rotate Elf1Pos 0
do_action rotate Elf1Pos 1
do_action rotate Elf1Pos 2
do_action walk WiggleEnde4Pos 3
do_action walk WiggleEnde5Pos 4

do_wait time 1
do_action rotate WiggleMitte1Pos 0
do_action rotate Elf1Pos 1
do_action rotate WiggleMitte1Pos 2
do_action rotate Elf1Pos 3
do_action rotate Elf1Pos 4
do_wait time 2
do_elf lookat Ring1Pos
do_wait time 1

#ELFE Hier!
elf unfollowview
sq_sound Hier 0
do_wait time 1

do_elf text 003f {reden_a|reden_b|reden_a|reden_b|reden_a|reden_b} Die
do_wait time 3
#Sie zaubert ZWEI RINGE vor den WIGGLE.

do_particle create 13 Ring1Pos {0 -0.1 0} 20 2
do_particle create 13 Ring1Pos {0 -0.2 0} 20 2
do_particle create 13 Ring1Pos {0 0 0} 20 2
do_particle create 13 Ring1Pos {0 -0.3 0} 20 2
do_particle create 13 Ring1Pos {0 -0.1 0.05} 20 2
do_particle create 13 Ring1Pos {0 -0.1 -0.05} 20 2
do_particle create 13 Ring1Pos {0 -0.2 -0.05} 20 2
do_particle create 13 Ring1Pos {0 -0.2 0.05} 20 2
-sound play magic_a 1
do_wait time 0.5
#+sq_pen move Elf1Pos {0 1 0}
#do_elf move Elf1Pos
do_wait time 1
+sq_object summon Ring_Der_Luft Ring1Pos
#+sq_pen move Elf1Pos {0 -1 0}
#do_elf move Elf1Pos
do_wait time 1

do_particle create 13 Ring2Pos {0 -0.1 0} 20 2
do_particle create 13 Ring2Pos {0 -0.2 0} 20 2
do_particle create 13 Ring2Pos {0 0 0} 20 2
do_particle create 13 Ring2Pos {0 -0.3 0} 20 2
do_particle create 13 Ring2Pos {0 -0.1 0.05} 20 2
do_particle create 13 Ring2Pos {0 -0.1 -0.05} 20 2
do_particle create 13 Ring2Pos {0 -0.2 -0.05} 20 2
do_particle create 13 Ring2Pos {0 -0.2 0.05} 20 2
-sound play magic_a 1
do_wait time 0.5
#+sq_pen move Elf1Pos {0 1 0}
#do_elf move Elf1Pos
do_wait time 1
+sq_object summon Ring_Der_Erde Ring2Pos
#+sq_pen move Elf1Pos {0 -1 0}
#do_elf move Elf1Pos
do_wait time 1

#ELFE Hat Odin mir gegeben. Das ist alles, was von Fenris' Halsband übrig ist. Der Ring der Luft und der Ring der Erde. Passt gut auf sie auf! Kapiert?!

do_action rotate 0 1
do_action rotate 1 0
do_wait time 0.5
do_action anim talkpapob 0
do_action anim talkpapob 1
do_wait time 0.5
do_action rotate Elf1Pos 0
do_action rotate Elf1Pos 1
do_wait time 0.5
do_action anim cheer 3
do_elf lookat
do_wait time 0.5

#WIGGLE Sind ja nicht blöde, wa!
+sq_pen set Elf1Pos 4
+sq_pen move Elf1Pos {3 -0.5 6}

sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c }
do_elf move Elf1Pos
sq_wait all
do_text 003g 2 PosAc Wir
#do_wait time 2

sq_wait none
#Elfe zeigt zum Stollen.
#do_elf text "Ich fliege zum Stollen, zeige nach unten..."
sq_camera selset inout
sq_camera move Elf1Pos 1.3 -0.2 0 0.4
do_action rotate front 0
do_action rotate front 1
do_action rotate front 2
do_action rotate front 3
do_action rotate front 4
do_wait time 3

#ELFE Irgendwo da unten muss es noch mehr Ringe geben!
sq_wait none
do_elf lookat 2
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
do_action rotate Elf1Pos 0
do_action rotate Elf1Pos 1
do_action rotate Elf1Pos 2
do_action rotate Elf1Pos 3
do_action rotate Elf1Pos 4
do_wait time 0.4

do_elf text 003h {reden_a} Daunten
do_wait time 2.1

do_elf lookat
do_wait time 0.5
#ELFE Habt ihr Euch ja ne Gott verlassene Senke ausgesucht. Naja, passt ja zu eurem mickrigen Lager...
#(zeigt zur Kiste)
+sq_pen set Elf1Pos DropPos
+sq_pen move Elf1Pos {-2 -0.5 0}

do_action walk WiggleKicher1Pos 0
do_action walk WiggleKicher2Pos 1
do_action walk WiggleKicher3Pos 2
do_action walk WiggleKicher4Pos 3
do_action walk WiggleKicher5Pos 4


elf unfollowview
do_elf move Elf1Pos
do_wait time 2
sq_camera selset inout
sq_camera move Elf1Pos 1.3 -0.25 0 0.4
do_wait time 2
do_elf text 003i {reden_a|reden_b|reden_a|reden_b} Kompliment
do_wait time 1.5;#5.5
do_action rotate front 1
do_wait time 2
#do_action rotate front 3
sq_actor actionlist 0 {{anim standloopa} {anim wait} {anim standloopc} {anim standloopb}}
sq_actor actionlist 1 {{anim standloopa} {anim wait} {anim standloopc}}
sq_actor actionlist 2 {{anim standloopb} {anim standloopc} {anim standloopd} {anim wait}}
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
do_action rotate front 0
do_action anim standloopb 1
do_action rotate front 2
do_wait time 1.5
do_action rotate front 3
do_elf lookat
do_wait time 1.2
#elf unfollowview
do_elf text 003j {zeigen_rechts|zustimmen|reden_a|reden_b|reden_a|reden_b} Schlagt;#9.5
do_action rotate front 4
do_wait time 3

#ELFE Könnt es hier ja aufschlagen und Euch dann umsehen. Eure schrecklichen Kollegen von den anderen Clans hocken bestimmt hier irgendwo in der Erde, vielleicht wissen die, wo Fenris und die Ringe stecken.

sq_actor actionlist 0 {{anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitstop}}
sq_actor actionlist 1 {{anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitstop}}
sq_actor actionlist 2 {{anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitstop}}

do_action anim knitstart 0
do_wait time 0.1
do_action anim knitstart 1
do_wait time 0.1
do_action anim knitstart 2
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
do_wait time 2
sq_actor actionlist 3 {{anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitstop}}
sq_actor actionlist 4 {{anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitloop} {anim knitstop}}
do_action anim knitstart 3
do_wait time 0.3
do_action anim knitstart 4
do_wait time 8

#Elfe Schüttelt angewidert den Kopf.
elf unfollowview
do_elf anim kopf_schuetteln
do_elf lookat 2
do_wait time 1
#ELFE Werds nie kapieren, wie man in der Erde leben kann... URGGGHHHH! Na! macht, was ihr am besten könnt: Mit DRECK spielen!
elf unfollowview
do_elf text 003k {ablehnen} Werds
do_wait time 9

do_elf lookat
do_wait time 0.4

#Sie zischt ab.
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
do_elf move ElfRausPos
do_action rotate left 4
do_wait time 1
do_text 003l 4 PosAc Bloede
do_wait time 3
#WIGGLE (ruft nach) Blöde Elfe! Donner doch gegen den Fels da...
+do_elf hide
do_wait time 1.5

#WIGGLE #2 Elfen <WÜRGGG>!
do_text 003m 2 {bend} Elfen
do_action rotate front 4
do_wait time 2.9

#MAN HÖRT AUS DEM OFF EIN KLATSCHEN UND EINEN AUFSCHREI DER ELFE. Sie IST gegen Fels gedonnert...
#do_elf text "Ich schreie auf, weil ich gegen einen Felsen geklatscht bin..."
#Hier fehlt noch ein Schrei der Elfe.
-sound play fe_schritt2 1
screenvibe 0 0.1 0.3 0.8 120 0.07 50
do_wait time 1

#Wiggles LACHEN/KICHERN, freuen sich...
sq_actor mouth 0 9
sq_actor mouth 1 9
sq_actor mouth 2 9
sq_actor mouth 3 9
sq_actor mouth 4 9
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 3 {c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c c 9 c }
sq_actor eyes 4 {c c c c c c c c c 9 c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
-sound play jubel_4o 1
-sound play lachen_6_m_mo 1
do_action anim talkrengc 0
do_action anim talkrengc 1
do_action anim talkrengc 2
do_action anim talkrengc 3
do_action anim talkrengc 4
do_wait time 3.5

#WIGGLE Auf geht's - holen wir Fenris aus der Unterwelt.
sq_actor mouth 0 0
sq_actor mouth 1 0
sq_actor mouth 2 0
sq_actor mouth 3 0
sq_actor mouth 4 0
do_text 003o 0 {talkc} So
do_wait time 3.2

+sq_camera move KameraEndePos 1.5 0 -0.25 0.5
+sq_wait camera

+do_action beam WiggleKicher1Pos 0
+do_action beam WiggleKicher2Pos 1
+do_action beam WiggleKicher3Pos 2
+do_action beam WiggleKicher4Pos 3
+do_action beam WiggleKicher5Pos 4

+cancel_fade
+sq_wait camera
+global viewpos; set viewpos {246.7 28.8 1.35 -0.25 0}
#+sq_camera get

#3a Ah! Hier muss es sein, wo Fenris ausgerissen ist.
#3b Na endlich! Lass uns hier graben!
#3c Joo!
#3d Ich soll auf euch aufpassen. Hmmm.-.. Eigentlich hab ich ja echt Besseres zu tun, als hier den Babysitter zu spielen!
#3e Hier!
#3f Hat Odin mir gegeben. Das ist alles, was von Fenris' Halsband übrig ist. Der Ring der Luft und der Ring der Erde. Passt gut auf sie auf! Kapiert?!
#3g Sind ja nicht blöde, wa!
#3h Irgendwo da unten muss es noch mehr Ringe geben!
#3i Habt ihr Euch ja ne Gott verlassene Senke ausgesucht. Naja, passt ja zu eurem mickrigen Lager...
#3j Könnt es hier ja aufschlagen und Euch dann umsehen. Eure schrecklichen Kollegen von den anderen Clans hocken bestimmt hier irgendwo in der Erde, vielleicht wissen die, wo Fenris und die Ringe stecken.
#3k Werds nie kapieren, wie man in der Erde leben kann... URGGGHHHH! Na! macht, was ihr am besten könnt: Mit DRECK spielen!
#3l Blöde Elfe! Donner doch gegen den Fels da...
#3m Elfen <WÜRGGG>!
#3n MAN HÖRT AUS DEM OFF EIN KLATSCHEN UND EINEN AUFSCHREI DER ELFE. Sie IST gegen einen Fels gedonnert...
#3o Auf geht's - holen wir Fenris aus der Unterwelt.


#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changemode normal
+adaptive_sound changethemenow start
+adaptive_sound primary start
#-----------------------------------------

