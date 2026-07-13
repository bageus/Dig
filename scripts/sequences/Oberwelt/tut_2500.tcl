#CLIP 2500 Tutorial ist beendet - Elfe kommt, sacht, dass Wettkampf losgeht!

#Anmelden des Textfiles und Audiofiles
sq_text file Tutorial
sq_audio open Clip_2050

#Szene-Camera: Wir sind unten bei den Strohpuppen....
+sq_pen set Puppe [Getobjpos Trainingspuppe 0 400]
+sq_pen set WigPos Puppe
+sq_pen set WigPos2 Puppe
+sq_pen set BeamPos Puppe

+sq_pen set VollbildKamera Puppe
+sq_pen set ElfKamera Puppe
+sq_pen set ElfPos Puppe
+sq_pen set ElfVorne Puppe
+sq_pen set ElfWartPos Puppe

+sq_pen move ElfPos {1 -0.5 3}
+sq_pen move ElfVorne {+1.4 -1 9}
+sq_pen move ElfWartPos {-3.0 -1.5 5}
+sq_pen move WigPos {-1 0 0}
+sq_pen move WigPos2 {-1.5 0 1}
+sq_pen move BeamPos {-4 0 4}

#+sq_pen move WigGross {2 0 -2}

# so , jetzt gehts ab!
sq_camera move VollbildKamera 1.1 -0.2 0 0.65
sq_actor find Zwerg 200 4 0
do_wait time 1.5

# Wiggle geht nach vorne und
#Elfe Action:Die Elfe kommt angeflogen
do_wait none
do_elf lookat 0
do_elf move ElfPos
do_action walk WigPos 0
do_wait time 3.3
do_action rotate ElfPos 0
global beam;if {$beam} {do_action beam BeamPos 1}
do_wait time 1.0

sq_wait none
do_elf anim anfeuern
sq_wait all
#Elfe Text: "So, Jungs! Toll gemacht! Könnt' Euch zwar noch mehr erklären - |aber das wird zuviel für Eure kleinen Köppe!"
do_elf text 2500a Auto So_Jungs

sq_wait none
# Gross, Wiggle erleichtert...
+sq_pen set WigGross WigPos
+sq_pen move WigGross {0.5 0 0}
#+sq_camera move WigGross 0.75 -0.1 -0.35
+sq_actor setrot 0 ElfPos
do_wait time 0.1

do_action walk WigPos2 1
do_action anim breathe 0
do_wait time 1.0

#Totale - Elfe haut ab....
+sq_pen move VollbildKamera {0 -0.6 0}
+sq_camera move VollbildKamera 1.2 0 0

do_elf move ElfVorne
do_wait time 0.5
do_elf lookat 0
do_action rotate ElfPos 0
do_wait time 4
do_action rotate ElfWartPos 0
do_elf hide
do_action anim cough  0
# Elfe kommt nochmal
do_wait time 3.0
do_elf move ElfWartPos
do_wait time 0.7
do_action rotate ElfWartPos 0
do_wait time 1.5
#Elfe text "Los! Kommt mit! Der Wettkampf fängt gleich an!"
do_elf text 2500b Auto Los_kommt
#sq_actor beam BeamPos 1
do_wait time 4.0
do_elf hide

#Elfe weg!
sq_pen set WigGross WigPos
#+sq_pen move WigGross {0.5 0 0}
sq_camera move WigGross 0.75 -0.1 -0.25
do_action anim talkgrng 0

do_action walk WigPos2 1

do_wait time 0.5
#Elfe text "JETZT weiss ich, weswegen Zwerge Elfe hassen!"

do_text 2500c 0 talkacngb Jetzt_weiss
do_action rotate 0 1
do_wait time 6.0
#Elfe text "Auch wenn sie keinen Euter hat..."
do_text 2500d 1 talkacpoa Auch_wenn
do_action rotate 1 0
do_wait time 5.0
#Elfe text "was für'ne BLÖDE, FLIEGENDE KUH!!!"
do_text 2500e 0 talkacngb Was_fuer

#Wir warten mal ordentlich ab... um zu sehen, obs geht
+sq_camera move VollbildKamera 1.1 -0.2 0 0.65
do_wait time 3.0

sq_actor express 9 0 0
sq_actor express 9 0 1
sq_actor express 9 0 2
sq_actor express 9 0 3
sq_sound Ha_Ha 0
do_action anim jumpa 0
do_action anim jumpa 1
do_action anim jumpa 2
do_action anim jumpa 3
do_wait time 1
sq_sound Ha_Ha 1
+sq_camera get
+start_fade 1 0
do_action anim cough 0
do_action anim cough 1
do_action anim cough 2
do_action anim cough 3
do_wait time 2

+option set showUI 0
+do_elf hide
+sq_object delete all

#sq_camera get


