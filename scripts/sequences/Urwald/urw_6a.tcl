#CLIP 6 ELFE KOMMT, ERKLÄRT GLEIPNIR

sq_text file Urwald
sq_audio open Clip_6a

sq_actor find Zwerg 600 5 0 TriggerPos
start_fade 0.5 0
do_wait time 2.5

#sq_actor find Zwerg 10 1

+sq_pen set Vollbild TriggerPos
+sq_pen move Vollbild {-15 0 0}

+sq_pen set Actor0 0
+catch {sq_pen set Actor1 1}
+catch {sq_pen set Actor2 2}
+catch {sq_pen set Actor3 3}
+catch {sq_pen set Actor4 4}

+sq_pen set Abseits1 TriggerPos
+sq_pen move Abseits1 {0 0 -2}
+sq_pen set Abseits2 TriggerPos
+sq_pen move Abseits2 {-1 0 -2}
+sq_pen set Abseits3 TriggerPos
+sq_pen move Abseits3 {-2 0 -2}
+sq_pen set Abseits4 TriggerPos
+sq_pen move Abseits4 {-3 0 -2}

catch {do_action beam Abseits1 1}
catch {do_action beam Abseits2 2}
catch {do_action beam Abseits3 3}
catch {do_action beam Abseits4 4}
do_action beam Vollbild 0

+sq_pen set SoundmarkerOberwelt { 257.5 30.5 14 }
+sq_pen set SoundmarkerUrwald SoundmarkerOberwelt
+sq_pen move SoundmarkerUrwald {0 10 0}

+sq_pen set KameraVollbild Vollbild
+sq_pen move KameraVollbild {-1 0 0}
+sq_pen set ElfPos Vollbild
+sq_pen move ElfPos {-3.0 -0.4 2}
+sq_pen set ElfBeamPos ElfPos
+sq_pen move ElfBeamPos {0 -6 0}
+sq_pen set ElfVornPos ElfPos
+sq_pen move ElfVornPos {0 0 6}
+sq_pen setz ElfVornPos 15

+sq_pen set HoehlenBilder [Getobjpos Info_Pos_Spinne 0 400]
+sq_pen set HoehlenBild1 HoehlenBilder
+sq_pen move HoehlenBild1 {-0.5 8 0}

+sq_pen set HoehlenBild2 HoehlenBild1
+sq_pen move HoehlenBild2 {4.7 -7.6 0}

+sq_pen set HoehlenBild3a HoehlenBild2
+sq_pen move HoehlenBild3a {1.4 -7.5 0}

+sq_pen set HoehlenBild3b HoehlenBild3a
+sq_pen move HoehlenBild3b {1.5 -2 0}

+sq_pen set HoehlenBild4 HoehlenBild3b
+sq_pen move HoehlenBild4 {8.3 6.5 0}

+sq_pen set HoehlenBild4b HoehlenBild4
+sq_pen move HoehlenBild4b {-1.5 -1 0}

+sq_pen set HoehlenBild5a HoehlenBild4
+sq_pen move HoehlenBild5a {2.5 -2.5 0};#3 1 0

+sq_pen set HoehlenBild5b HoehlenBild4
+sq_pen move HoehlenBild5b {-1.2 6.3 0}

+sq_pen set HoehlenBild6a HoehlenBild5b
+sq_pen move HoehlenBild6a {-4.7 4 0}

+sq_pen set HoehlenBild6b HoehlenBild6a
+sq_pen move HoehlenBild6b {-1 4 0}

+sq_pen set HoehlenBild6c HoehlenBild6b
+sq_pen move HoehlenBild6c {-5.75 6 0}

+sq_pen set HoehlenBild6d HoehlenBild6b
+sq_pen move HoehlenBild6d {-5.25 -1 0}

+sq_camera fix KameraVollbild 1.3 -0.2 0 0.5

#ELFE kommt angeflogen (vielleicht immer da hin, wo Spieler gerade mit Wiggle ist - oder zur Feuerstelle)
do_elf beam ElfBeamPos
do_wait time 0.5
#sq_actor actionlist 0 {{}}
sq_actor eyes 0 {c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c 9 c c c c c 9 c c c c 9 c c c c c 9 c c }
sq_actor express 0 good_normal
do_action anim standloopa 0
do_wait time 1.0

+catch {del [Getobjref FogRemover]}

+start_fade 1 1

#do_elf move ElfPos
elf movescreen {150 250 -16}
do_wait time 4

#ELFE Na Wichtels - äh Wiggles!?! Ringe schon gefunden?...
+sq_pen set Wiggle1Pos ElfPos
+sq_pen move Wiggle1Pos {1 0 0}

#do_action walk Wiggle1Pos 0

+sq_pen set Wiggle1Pos 0
+sq_pen move Wiggle1Pos {-2 0 0}

+sq_camera move Wiggle1Pos 1.05 -0.2 -0.3 0.5
do_elf lookat 0
do_wait time 3
elf unfollowview
do_elf text 006aa {reden_a} NaWichtels
do_action anim jumpa 0
do_wait time 2
do_action rotate ElfPos 0
do_wait time 2.8

#WIGGLE Willst denn du schon wieder!
sq_wait none
do_text 006ab 0 Auto WasWillst Auto
#do_text 006ad 0 NegReac Verschon Auto Off
do_wait time 2.2

#ELFE Dir 'n Tipp geben?
elf unfollowview
do_elf text 006ac {reden_b} Dir
do_wait time 2.4

#WIGGLE Verschon mich
do_text 006ad 0 NegReac Verschon
do_wait time 1.5

#ELFE Dann nicht! Willst mich nicht dabei haben - okay!
elf unfollowview
do_elf text 006ae {schmollen} Schoen
do_wait time 3;#5.5

#ELFE Aber um Fenris zu bändigen, musst du Ringe sammeln. Spüre sie auf, sammle sie.
do_elf unfollowview
do_elf text 006af {zustimmen} Aber
do_wait time 2.5


#WIGGLE Und dann?!? Sollen wir uns die Dinger durch die Nase stecken, oder was!?
do_text 006ag 0 Auto UndDann
do_elf lookat
do_wait time 4.7

#ELFE (hönisch) Hahaha...
sq_camera move Vollbild 1.4 -0.2 0 0.1
elf unfollowview
do_elf text 006ah {kichern} Haha
do_wait time 1.2

#Die Elfe fliegt nach vorne:
#do_elf move ElfVornPos
#elf movescreen {150 250 -17}

do_wait time 1
do_action rotate ElfVornPos 0
do_wait time 1

#ELFE (CONT'D) Vor tausenden von Jahren schmiedeten eure Vorfahren, eine mächtige Kette. Eine Kette aus 6 Ringen.... 6 Ringe, die ...
elf unfollowview
do_elf text 006ai {reden_a|reden_a} Vor
do_wait time 8

sq_color 0 Offtext

#Fade_out
start_fade 2 0
do_wait time 2
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow gleipnir
#-----------------------------------------
#######################################################################################
# Maerchenerzähler
######################################################################################

#fade in
#1. BILD: ZWERGE AN EINEM AMBOSS, SCHMIEDEN...
sq_camera fix HoehlenBild1 1.3 0 0

#do_text "Kameraschnitt auf erstes 2D-Wandgemälde" 0
#MÄRCHEN-ERZÄHLER (O.S.) ... zu einem mächtigen Band geschmiedet wurden. Unzerstörbar, unendlich haltbar...

start_fade 2 1

do_text 006ba 0 {} ZuEinem Auto Off
do_wait time 1

#2. BILD: NÄHER DRAN: SCHMIEDE HAMMER AUF AMBOSS ETC...
#MÄRCHEN-ERZÄHLER (O.S.) (CONT'D) ... - Dank der Schmiedekunst und alten Magie der Zwerge... Das GLEIPNIR!
sq_camera selset inout
sq_camera move HoehlenBild2 1.3 0 0 0.1
do_wait time 7.5;#8.5

do_text 006bb 0 {} Dank Auto Off
do_wait time 4.5;#3.5

#3. BILD: EINE MAGISCHE Kette erscheint - das GLEIPNIR:
#MÄRCHEN-ERZÄHLER (O.S.) (CONT'D) Odin gelang es, den Höllenhund Fenris damit zu bannen -
sq_camera selset inout
sq_camera move HoehlenBild3a 1.15 0 0 0.1
do_wait time 5;#6

do_text 006bc 0 {} Damit Auto Off
sq_camera selset inout
sq_camera move HoehlenBild3b 1.75 0 0 0.2
do_wait time 8;#7

#(lapidarer) Tja, nun ist er ausgebüchst. Konnte das Gleipnir abstreifen beim Gassi-Gehen. Hat der Odin wohl ein Problem... ähh, wo waren wir.. Ach ja...
sq_camera selset inout
sq_camera move HoehlenBild4 1.35 0 0 0.15
do_wait time 2.1

do_text 006bd 0 {} Tja Auto Off
do_wait time 4.5
sq_camera move HoehlenBild5a 0.7 0 0 0.1
do_wait time 2.3

#4: DAS GLEIPNIR zerbricht
#Märchenerzähler:
do_text 006bdb 0 {} Auto Auto Off
do_wait time 3
sq_camera selset inout
sq_camera move HoehlenBild4b 1.35 0 0 0.5 ;#äh Wo waren wir
do_wait time 3.1;#3 ;#2.5

#5: Die Kettenglieder reissen auseinander.
#Märchenerzähler:
do_text 006be 0 {} Jahrtausende Auto Off;#Jahrtausende ...
sq_camera selset inout
sq_camera move HoehlenBild5b 1.2 0 0 0.13
do_wait time 7.3
do_text 006beb 0 {} Auto Auto Off;#Doch jetzt
do_wait time 0.9

sq_camera selset inout
sq_camera move HoehlenBild6a 1.4 0 0 0.35
#do_wait time 1
do_wait time 2.6

do_text 006bec 0 {} Auto Auto Off;#Es zerbrach, zerfiel ...
sq_camera selset inout
sq_camera move HoehlenBild6c 1.6 0 0 0.16
do_wait time 4
sq_camera selset inout
sq_camera move HoehlenBild6d 2 0 0 0.3
do_wait time 7

start_fade 8 0
do_text 006bf 0 {} UndDas Auto Off
do_wait time 6

do_text 006bg 0 {} IchGlaube
#sq_camera selset inout
#sq_camera move HoehlenBild6d 2.0 0 0 0.1
sq_camera move HoehlenBild6d 2 0 0 0.1
+sq_pen move ElfVornPos {-5 0 0}
#do_elf move ElfVornPos
elf movescreen {80 250 -16}
do_wait time 3.9

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changetheme cave
#-----------------------------------------
#fade out

#6: DIE RINGE FALLEN, stürzen....
#MÄRCHEN-ERZÄHLER (O.S.) (CONT'D) Irgendwo in der Unterwelt warten die 6 Ringe auf ihren Finder... Die Wiggels. Dass sie das Gleipnir erneut schmieden. Dieses unzerreissbare Band, um Fenris an die Kette zu legen. Endgültig!!!
sq_actor eyes 0 {c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c c 9 c c c c c 9 c c c c 9 c c c c c 9 c c }
do_text 006bh 0 {} Irgendwo Auto Off
sq_actor actionlist 0 {{anim leftright} {rotate front} {anim scratchhead} {anim lookup} {anim leftright} {anim dontknow} {anim scout} {anim bend} {anim scratchhead} {anim teeter_w} {anim wait} {anim cheer} {anim leftright}}
+do_action beam Wiggle1Pos 0
+set_visibility [Actor 0] 1
+do_action rotate ElfVornPos 0
#fade in
+start_fade 5 1

+reset_map 23 23 70 70
#.... 7,8,9 sie werden kleiner, Ringe verschwinden im Nichts.

sq_camera fix 0 0.7 -0.2 0 0.4
do_elf lookat 0
do_wait time 9
do_text 006bhb 0 {} Auto Auto Off
sq_actor eyes 0 {3 3 9 3 3 3 9 3 3 3 9 3 3 3 9 3 3 3 9 3 3 9 3 3 3 3 9 3 3 3 3 3 9 3 3 3 3 3 3 9 3 3 9 3 3 3 3 3 3 3 3 9 3 3 3 3 3 3 3 9 3 3 9 3 3 3 3 3 3 3 3 9 3 3 3 3 3 3 3 9 3 3 9 3 3 3 3 9 3 3 3 3 9 3 3 3 3 9 3 3 3 9 3 3 9 3 3 3 3 9 3 3 3 3 9 3 3 3 3 3 3 3 9 3 3 9 3 3 3 3 3 3 3 3 9 c c c c c c c 9 c c 9 c c c c c c c c 9 c c c c c 9}
do_wait time 6

do_wait time 7

sq_camera move 0 1.4 -0.2 0 0.4
+sq_pen move ElfVornPos {1 0 0}
#do_elf move ElfVornPos
do_wait time 0.5
#Elfe zu Wiggel: ELFE (ironisch) Aber du schaffst da ja, na klar!
elf unfollowview
do_elf text 006ca {anfeuern} NaKlingt
do_wait time 2.7

do_elf lookat
do_action rotate front 0
do_wait time 2
#Elfe direkt zu Spieler: ELFE Hast du ja ne Brut am Hals... man oh man... |Ich hau lieber ab.
elf unfollowview
do_elf text 006cb {auffordern} DieWerden
do_wait time 3.8

start_fade 1 0

#Sie düst ab.
+do_elf hide
+do_action beam Actor0 0
+catch {do_action beam Actor1 1}
+catch {do_action beam Actor2 2}
+catch {do_action beam Actor3 3}
+catch {do_action beam Actor4 4}
do_wait time 3

+start_fade 1 1

#del [Getobjref Hoehlenmalerei]
+sq_sound Nichts 0
+sq_object delete all


#+sq_camera get

#006aa Na Wichtels - äh Wiggles!?! Ringe schon gefunden?...
#006ab Willst denn du schon wieder!
#006ac Dir 'n Tipp geben?
#006ad Brauch ich nicht!
#006ae Dann nicht! Willst mich nicht dabei haben - okay!
#006af Aber um Fenris zu bändigen, musst du Ringe sammeln. Spüre sie auf, sammle sie.
#006ag Und dann?!? Sollen wir uns die Dinger durch die Nase stecken, oder was!?
#006ah Hahaha...
#006ai Vor tausenden von Jahren schmiedeten eure Vorfahren, eine mächtige Kette. |Eine Kette aus 6 Ringen.... 6 Ringe, die ...

#006ba ... zu einem mächtigen Band geschmiedet wurden. Unzerstörbar, unendlich haltbar...
#006bb ... - Dank der Schmiedekunst und alten Magie der Zwerge... Das GLEIPNIR!
#006bc Odin gelang es, den Höllenhund Fenris damit zu bannen -
#006bd Tja, nun ist er ausgebüchst. Konnte das Gleipnir abstreifen beim Gassi-Gehen. Hat der Odin wohl ein Problem... ähh, wo waren wir.. Ach ja...
#006be Getrennt von Fenris Hals jedoch zerfiel das Gleipnir. Es zerbarste, es zerbrach...
#006bf Jahrtausende waren seit dem Schmieden verstrichen, und das Wissen um das Gleipnir versiegt - verscholl wie jetzt die Ringe dieses mächtigen Bands...
#006bg Irgendwo in der Unterwelt warten die 6 Ringe auf ihren Finder... Die Wiggels. Dass sie das Gleipnir erneut schmieden. Dieses unzerreissbare Band, um Fenris an die Kette zu legen. Endgültig!!!

#006ca Aber du schaffst das ja, na klar!
#006cb Hast du ja ne Brut am Hals... man oh man... |Ich hau lieber ab.

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow cave
+adaptive_sound marker cave [get_pos this] 1000
#-----------------------------------------

+cancel_fade
