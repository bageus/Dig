#650 EXTRO
sq_text file Urwald


sq_pen set BodenPos [Getobjpos Info_Pos_Zwerg 20]
sq_pen move BodenPos {0 0 10}

sq_pen set OdinKopfPos BodenPos
sq_pen move OdinKopfPos {0 -8 0}
sq_pen set OdinNormalPos BodenPos
sq_pen move OdinNormalPos {0 -2 0}

sq_pen set KoenigTalkPos BodenPos
sq_pen move KoenigTalkPos {-1 0 3}

+sq_pen set WalkStartPos BodenPos
+sq_pen move WalkStartPos {-8 0 0}
+sq_pen set Wiggle1Pos WalkStartPos
+sq_pen move Wiggle1Pos {0 0 0}
+sq_pen set Wiggle2Pos WalkStartPos
+sq_pen move Wiggle2Pos {-1 0 0}
+sq_pen set Wiggle3Pos WalkStartPos
+sq_pen move Wiggle3Pos {-2 0 0}
+sq_pen set Wiggle4Pos WalkStartPos
+sq_pen move Wiggle4Pos {-3 0 0}
+sq_pen set Wiggle5Pos WalkStartPos
+sq_pen move Wiggle5Pos {-4 0 0}
+sq_pen set Wiggle6Pos WalkStartPos
+sq_pen move Wiggle6Pos {-5 0 0}
+sq_pen set Wiggle7Pos WalkStartPos
+sq_pen move Wiggle7Pos {-6 0 0}
+sq_pen set Wiggle8Pos WalkStartPos
+sq_pen move Wiggle8Pos {-7 0 0}
+sq_pen set Wiggle9Pos WalkStartPos
+sq_pen move Wiggle9Pos {-8 0 0}
+sq_pen set Wiggle10Pos WalkStartPos
+sq_pen move Wiggle10Pos {-9 0 0}
+sq_pen set Wiggle11Pos WalkStartPos
+sq_pen move Wiggle11Pos {-10 0 0}
+sq_pen set Wiggle12Pos WalkStartPos
+sq_pen move Wiggle12Pos {-11 0 0}
+sq_pen set Wiggle13Pos WalkStartPos
+sq_pen move Wiggle13Pos {-12 0 0}
+sq_pen set Wiggle14Pos WalkStartPos
+sq_pen move Wiggle14Pos {-13 0 0}
+sq_pen set Wiggle15Pos WalkStartPos
+sq_pen move Wiggle15Pos {-14 0 0}
+sq_pen set Wiggle16Pos WalkStartPos
+sq_pen move Wiggle16Pos {-15 0 0}
+sq_pen set Wiggle17Pos WalkStartPos
+sq_pen move Wiggle17Pos {-16 0 0}
+sq_pen set Wiggle18Pos WalkStartPos
+sq_pen move Wiggle18Pos {-17 0 0}

+sq_pen set WiggleOdin1Pos WalkStartPos
+sq_pen move WiggleOdin1Pos {17 0 0}
+sq_pen set WiggleOdin2Pos WiggleOdin1Pos
+sq_pen move WiggleOdin2Pos {-1 0 0}
+sq_pen set WiggleOdin3Pos WiggleOdin2Pos
+sq_pen move WiggleOdin3Pos {-1 0 0}
+sq_pen set WiggleOdin4Pos WiggleOdin3Pos
+sq_pen move WiggleOdin4Pos {-1 0 0}
+sq_pen set WiggleOdin5Pos WiggleOdin4Pos
+sq_pen move WiggleOdin5Pos {-1 0 0}
+sq_pen set WiggleOdin6Pos WiggleOdin5Pos
+sq_pen move WiggleOdin6Pos {-1 0 0}
+sq_pen set WiggleOdin7Pos WiggleOdin6Pos
+sq_pen move WiggleOdin7Pos {-1 0 0}
+sq_pen set WiggleOdin8Pos WiggleOdin7Pos
+sq_pen move WiggleOdin8Pos {-1 0 0}
+sq_pen set WiggleOdin9Pos WiggleOdin8Pos
+sq_pen move WiggleOdin9Pos {-1 0 0}
+sq_pen set WiggleOdin10Pos WiggleOdin9Pos
+sq_pen move WiggleOdin10Pos {-1 0 0}
+sq_pen set WiggleOdin11Pos WiggleOdin10Pos
+sq_pen move WiggleOdin11Pos {-1 0 0}
+sq_pen set WiggleOdin12Pos WiggleOdin11Pos
+sq_pen move WiggleOdin12Pos {-1 0 0}
+sq_pen set WiggleOdin13Pos WiggleOdin12Pos
+sq_pen move WiggleOdin13Pos {-1 0 0}
+sq_pen set WiggleOdin14Pos WiggleOdin13Pos
+sq_pen move WiggleOdin14Pos {-1 0 0}
+sq_pen set WiggleOdin15Pos WiggleOdin14Pos
+sq_pen move WiggleOdin15Pos {-1 0 0}
+sq_pen set WiggleOdin16Pos WiggleOdin15Pos
+sq_pen move WiggleOdin16Pos {-1 0 0}
+sq_pen set WiggleOdin17Pos WiggleOdin16Pos
+sq_pen move WiggleOdin17Pos {-1 0 0}
+sq_pen set WiggleOdin18Pos WiggleOdin17Pos
+sq_pen move WiggleOdin18Pos {-1 0 0}

+sq_pen set KameraTalkNahPos OdinNormalPos
+sq_pen move KameraTalkNahPos {0 -2 0}
+sq_pen set KameraTalkFernPos OdinNormalPos
+sq_pen move KameraTalkFernPos {0 0 0}

#Erstmal Odin Geschichten... Nebenbei Wiggles Summonen
sq_camera fix OdinKopfPos 1.5 0 0


#ODIN Ungezogener kleiner Fenris... Du-du-du... Einfach so abzuhauen !
do_action beam Wiggle1Pos 0
sq_object summon Zwerg Wiggle2Pos
call_method [Object 0] init
do_wait time 0.2
sq_object summon Zwerg Wiggle3Pos
call_method [Object 1] init
do_wait time 0.2
sq_object summon Zwerg Wiggle4Pos
call_method [Object 2] init
do_wait time 0.2
sq_object summon Zwerg Wiggle5Pos
call_method [Object 3] init
do_wait time 0.2
sq_object summon Zwerg Wiggle6Pos
call_method [Object 4] init
do_wait time 0.2
sq_object summon Zwerg Wiggle7Pos
call_method [Object 5] init
do_wait time 0.2
sq_object summon Zwerg Wiggle8Pos
call_method [Object 6] init
do_wait time 0.2
sq_object summon Zwerg Wiggle9Pos
call_method [Object 7] init
do_wait time 0.2
sq_object summon Zwerg Wiggle10Pos
call_method [Object 8] init
do_wait time 0.2
sq_object summon Zwerg Wiggle11Pos
call_method [Object 9] init
do_wait time 0.2
sq_object summon Zwerg Wiggle12Pos
call_method [Object 10] init
do_wait time 0.2
sq_object summon Zwerg Wiggle13Pos
call_method [Object 11] init
do_wait time 0.2
sq_object summon Zwerg Wiggle14Pos
call_method [Object 12] init
do_wait time 0.2
sq_object summon Zwerg Wiggle15Pos
call_method [Object 13] init
do_wait time 0.2
sq_object summon Zwerg Wiggle16Pos
call_method [Object 14] init
do_wait time 0.2
sq_object summon Zwerg Wiggle17Pos
call_method [Object 15] init
do_wait time 0.2
sq_object summon Zwerg Wiggle18Pos
call_method [Object 16] init
do_wait time 0.2

sq_object summon Dummy_Krone 0
link_obj [Object 17] [Actor 0] 11
sq_actor find Zwerg 40 20
do_wait time 6

#Die Wiggles warten vor dem Thron.

do_wait time 2
####################################

sq_camera move KameraTalkFernPos 2.0 -0.1 0 0.2

sq_wait none
do_action walk WiggleOdin1Pos 0
do_action walk WiggleOdin2Pos 1
do_action walk WiggleOdin3Pos 2
do_action walk WiggleOdin4Pos 3
do_action walk WiggleOdin5Pos 4
do_action walk WiggleOdin6Pos 5
do_action walk WiggleOdin7Pos 6
do_action walk WiggleOdin8Pos 7
do_action walk WiggleOdin9Pos 8
do_action walk WiggleOdin10Pos 9
do_action walk WiggleOdin11Pos 10
do_action walk WiggleOdin12Pos 11
do_action walk WiggleOdin13Pos 12
do_action walk WiggleOdin14Pos 13
do_action walk WiggleOdin15Pos 14
do_action walk WiggleOdin16Pos 15
do_action walk WiggleOdin17Pos 16
do_action walk WiggleOdin18Pos 17
do_wait time 25


#Der ZWERGENKÖNIG kommt ins Bild.
sq_pen set TalkerPos OdinNormalPos
sq_pen move TalkerPos {0 2 1}

do_action rotate back 1
do_action rotate back 2
do_action rotate back 3
do_action rotate back 4
do_action rotate back 5
do_action rotate back 6
do_action rotate back 7
do_action rotate back 8
do_action rotate back 9
do_action rotate back 10
do_action rotate back 11
do_action rotate back 12
do_action rotate back 13
do_action rotate back 14
do_action rotate back 15
do_action rotate back 16

sq_camera follow 0 1.2
do_action run OdinNormalPos 0
do_action beam TalkerPos 17
do_wait time 5

#ZWERGENKÖNIG Hallo!? Odin? Die Belohnung?!?!
do_action rotate front 0
do_action rotate front 17
do_wait time 1

do_text 650b 0 Auto
do_wait time 4

#ODIN Macht euch keine Sorgen, die Elfe hat alles ordnungsgemäß erhalten. Ja... ähm... irgendjemand muss wohl die Walküren rufen und, also, es ist... ich muß weg.
do_text "ODIN Macht euch keine Sorgen, die Elfe hat alles ordnungsgemäß erhalten. |Ja... ähm... irgendjemand muss wohl die Walküren |rufen und, also, es ist... ich muß weg." 17 Auto
do_wait time 8

sq_camera fix KameraTalkFernPos 2.0 0 0 0.2
#Die Wiggles sehen sich verwirrt an, aber Odin nickt noch mal zum Gruss und - FFFUUUMMMPPP - löst sich samt Fenris auf.
do_action rotate left 1
do_action rotate right 2
do_action rotate left 3
do_action rotate right 4
do_action rotate left 5
do_action rotate right 6
do_action rotate left 7
do_action rotate right 8
do_action rotate left 9
do_action rotate right 10
do_action rotate left 11
do_action rotate right 12
do_action rotate left 13
do_action rotate right 14
do_action rotate left 15
do_action rotate right 16
do_wait time 1
do_action rotate left 2
do_action rotate right 1
do_action rotate left 4
do_action rotate right 3
do_action rotate left 6
do_action rotate right 5
do_action rotate left 8
do_action rotate right 7
do_action rotate left 10
do_action rotate right 9
do_action rotate left 12
do_action rotate right 11
do_action rotate left 14
do_action rotate right 13
do_action rotate left 16
do_action rotate right 15
#Stille...
do_wait time 1
do_action anim scratchhead 0
do_action anim scratchhead 1
do_action anim scratchhead 2
do_action anim scratchhead 3
do_action anim scratchhead 4
do_action anim scratchhead 5
do_action anim scratchhead 6
do_action anim scratchhead 7
do_action anim scratchhead 8
do_action anim scratchhead 9
do_action anim scratchhead 10
do_action anim scratchhead 11
do_action anim scratchhead 12
do_action anim scratchhead 13
do_action anim scratchhead 14
do_action anim scratchhead 15
do_action anim scratchhead 16
do_action anim scratchhead 17
do_wait time 1
do_action rotate front 1
do_action rotate front 2
do_action rotate front 3
do_action rotate front 4
do_action rotate front 5
do_action rotate front 6
do_action rotate front 7
do_action rotate front 8
do_action rotate front 9
do_action rotate front 10
do_action rotate front 11
do_action rotate front 12
do_action rotate front 13
do_action rotate front 14
do_action rotate front 15
do_action rotate front 16
do_wait time 1



#Die Wiggles stehen fragend vor Odins leerem Thron. Sie kratzen sich verwirrt am Kopf, blicken sich wartend um - nix! Der Typ ist echt abgehauen!
#Die sonore Stimme des Märchen-Erzähler setzt ein.
sq_camera move 0 1.2 -0.1 0 1.1

#MÄRCHEN-ERZÄHLER (OFF) Und so lebten und liebten die Wiggles weiter unter der der Kruste der Erde. Sie hatten ihre große Aufgabe bewältigt und waren froh ---
do_text "MÄRCHEN-ERZÄHLER (OFF) Und so lebten und liebten die Wiggles weiter |unter der der Kruste der Erde. Sie hatten ihre große Aufgabe bewältigt |und waren froh ---" 17 Auto
do_wait time 8

#Der Zwergenkönig blickt zornig nach oben.
#ZWERGENKÖNIG #(unterbricht) 'FROH'?!? Du Trollkopp! Reingelegt worden sind sie mal wieder! Reingelegt!
do_text "'FROH'?!? Du Trollkopp! Reingelegt worden sind sie mal wieder! |Reingelegt!" 0 NegReac
do_wait time 8

#MÄRCHENERZÄHLER (off) Reingelegt? Wieso Reingelegt?
do_text "MÄRCHENERZÄHLER (off) Reingelegt? Wieso Reingelegt?" 17 Auto
do_wait time 4

#ZWERGENKÖNIG Reingelegt, na im Sinne von, über's Ohr gehauen, betrogen, verarscht, über'n Tisch gezogen, angemeiert.
do_text "Reingelegt, na im Sinne von, über's Ohr gehauen, betrogen, verarscht,| über'n Tisch gezogen, angemeiert." 0 Auto
do_wait time 8

#MÄRCHENERZÄHLER Aber das ist ja schrecklich!
do_text "#MÄRCHENERZÄHLER Aber das ist ja schrecklich!" 17 PosReac
do_wait time 5

#ZWERGENKÖNIG Das kann man wohl sagen
do_text "Das kann man wohl sagen" 0 PosReac
do_wait time 5

#MÄRCHENERZÄHLER Das ist... Das mach ich nicht. Das ist doch ein Scheiß-Märchen. Sowas erzähl ich doch nicht. Vor all den Leuten.
do_text "Das ist... Das mach ich nicht. Das ist doch ein Scheiß-Märchen. |Sowas erzähl ich doch nicht. Vor all den Leuten." 17 0 Auto
do_wait time 8

#ODIN (OFF) Also gut. Wenn das so ist: Dann wollen mal nicht so sein! Gute Arbeit - Jungs!
do_text "ODIN (OFF) Also gut. Wenn das so ist: Dann wollen mal |nicht so sein! Gute Arbeit - Jungs! " 17 Auto
do_wait time 8

sq_camera fix KameraTalkFernPos 2.0 0 0 0.2
do_wait camera

do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 0 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 1 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 2 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 3 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 4 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 5 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 6 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 7 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 8 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 9 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 10 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 11 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 12 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 13 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 14 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 15 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 16 PosReac
do_text "PPPPPPPAAAAAAAAARRRRRRRRRRTTTTTTTTTTTTTTTTYYYYYYYYYYYYYYY!!!!" 17 PosReac
do_wait time 7

sq_object summon Brauerei OdinKopfPos
do_wait time 0.3
sq_object summon Brauerei WiggleStartPos
do_wait time 0.3

sq_object move [Object 18] OdinNormalPos
sq_object move [Object 19] TalkerPos
do_wait time 10

sq_camera fix KameraTalkFernPos 2.0 0 0 0.2
do_wait camera

+sq_object delete all
+sq_camera get

#650a Ungezogener kleiner Fenris... Du-du-du... Einfach so abzuhauen !
#650b Hallo!? Odin? Die Belohnung?!?!
#650c Macht euch keine Sorgen, die Elfe hat alles ordnungsgemäß erhalten. Ja... ähm... irgendjemand muss wohl die Walküren rufen und, also, es ist... ich muß weg.
#650d Und so lebten und liebten die Wiggles weiter unter der der Kruste der Erde. Sie hatten ihre große Aufgabe bewältigt und waren froh ---
#650e 'FROH'?!? Du Trollkopp! Reingelegt worden sind sie mal wieder! Reingelegt!
#650f Reingelegt? Wieso Reingelegt?
#650g Reingelegt, na im Sinne von, über's Ohr gehauen, betrogen, verarscht, über'n Tisch gezogen, angemeiert.
#650h Aber das ist ja schrecklich!
#650i Das kann man wohl sagen
#650j Das ist... Das mach ich nicht. Das ist doch ein Scheiß-Märchen. Sowas erzähl ich doch nicht. Vor all den Leuten.
#650k Also gut. Wenn das so ist: Dann wollen mal nicht so sein! Gute Arbeit - Jungs!


