#CLIP 701 - SCHWEFELBRÜCKE 1
#Man braucht mindestens 3 Zwerge

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s701
#-----------------------------------------

log "Bruecke 1"

#Anmelden des Textfiles und Audiofiles
+sq_text file Schwefel
sq_audio open Clip_701

#Kameras und Positionen

+sq_pen set BasePos	TriggerPos
+sq_pen move BasePos {-0.2 0 1.5 }

#+sq_pen set VollbildKamera TriggerPos
#+sq_pen set EstablishingShot TriggerPos
#+sq_pen set WiggleRightPos1 TriggerPos
#+sq_pen set WiggleRightPos2 TriggerPos
#+sq_pen set WiggleRightPos2vorn TriggerPos
#+sq_pen set BrueckeRightPos1 TriggerPos
#+sq_pen set SummonPos TriggerPos
#+sq_pen set SummonPilzStammPos TriggerPos
#+sq_pen set WiggleRightDropZone TriggerPos

+sq_pen set VollbildKamera BasePos
+sq_pen set EstablishingShot BasePos
+sq_pen set WiggleRightPos1 BasePos
+sq_pen set WiggleRightPos2 BasePos
+sq_pen set WiggleRightPos2vorn BasePos
+sq_pen set BrueckeRightPos1 BasePos
+sq_pen set SummonPos BasePos
+sq_pen set SummonPilzStammPos BasePos
+sq_pen set WiggleRightDropZone BasePos

+sq_pen move VollbildKamera {2.5 -0.2 3}
+sq_pen move EstablishingShot {17 0 2.3}
+sq_pen move WiggleRightPos1 {4.5 0 2.3}
+sq_pen move WiggleRightPos2 {3.0 0 2.3}
+sq_pen move WiggleRightPos2vorn {2.5 0 1.8}
+sq_pen move BrueckeRightPos1 {23 -0.7 0};#0.5=y
+sq_pen move SummonPos {-4 0 -4}
+sq_pen move SummonPilzStammPos {-4 0 -4}
+sq_pen move WiggleRightDropZone {0.8 0 -2}
#sq_pen move SummonPos {3.5 0 2.3}

#sq_camera move VollbildKamera 1.4 -0.2 0


#Wiggle 1 Action: Spieler-Wiggle kommt an zerstörte Brücke und bekommt einen Schreck
#und
#Wiggle 1 Action: geht GANZ bis zum Rand (oder bis dahin, wo er noch auf der kaputten Brücke gehen kann). Der Wiggle #1 schaut runter in die Tiefe.
sq_wait none
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }

do_action anim scout 0
do_wait time 0.7
do_action run WiggleRightPos1 0
do_wait time 3;# Zeit damit Synchron mit Wiggle move
#sq_camera fix VollbildKamera 1.6 -0.2 0.3 ;# Damit kommt man zum gedachten Bildschirm
sq_camera move VollbildKamera 1.6 -0.2 0.3 0.7
do_wait time 3.3

do_action anim impatient 0
do_wait time 0.3

#Kameralauf entlang der Bruecke
sq_wait none
sq_camera move BrueckeRightPos1 1.6 -0.35 -1.0 0.2;# zoom 1.3
#do_action anim scratchhead 0
do_wait time 10
do_action anim cheer 0
do_wait time 0.7
do_action anim cheer 0
do_wait time 0.7

#Kamerabild Relationsbild Brücke - Wiggle
#sq_camera fix EstablishingShot 2.5 -0.35 0.2;#0.47
#do_wait time 2

#sq_camera move VollbildKamera 1.4 -0.2 0.3 0.3
sq_camera fix VollbildKamera 1.2 -0.2 0.3 0.3
do_action rotate front 0
do_wait time 0.4
do_action anim scratchhead 0
do_wait time 0.5
do_action rotate left 0
do_wait time 0.2

#Wiggle 2 Action: kommt, stellt sich stumm neben den ersten, der immer noch runtersieht. Wiggle #2 tut es ihm stumm gleich. Beide sehen in die Tiefe.
sq_object summon Zwerg SummonPos 6
call_method [Object 0] Editor_Set_Info {{name Ati} {gender female}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg 25 1 6
do_wait time 0.3
do_change muetze arbeitslos 1 auf noanim
do_wait time 0.2

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

do_action run WiggleRightPos2vorn 1
sq_actor actionlist 0 {{anim wait} {anim showright} {anim wipenose} {anim leftright}}
do_action anim scratchhead 0
do_wait time 4

#do_action anim mann.unterhalten_g 1
do_action anim cheer 1
do_wait time 1

do_action run WiggleRightPos2 1
do_wait time 2

do_action rotate right 0
do_action anim cough 1
do_wait time 0.7

#Wiggle 2 Text: Zehn Pilzstämme.
do_text 701a 1 PosAc Zehn
do_wait time 2

do_action anim cough 0
do_wait time 0.4

#Wiggle 1 Text: Hmm.. 15 Stämme - sicher ist sicher.
do_text 701b 0 NegReac Fuenfzehn
do_wait time 4.5

#Dann sehen sich gleichzeitig an.
#Wiggle 1 action: schaut auf Wiggle 2
#Wiggle 2 action: schaut auf Wiggle 1
do_action rotate 1 0
do_action anim wait 1
do_wait time 1

#Wiggle 1 Text: Jooo! Okay!
do_text 701c 0 {{talkrengaq}} NaDenn
do_wait time 0.5
do_change muetze arbeitslos 1 ab
do_wait time 0.3
do_change muetze wood 1
do_wait time 1.5

#Wiggle 2 Action: SPRINGT fröhlich davon...
do_action rotate SummonPos 1
do_wait time 0.3
#sq_actor actionlist 1 {{anim mann.skipp_start} {anim mann.skipp_loop} {anim mann.skipp_loop}  {anim mann.skipp_loop}  {anim mann.skipp_loop} }
#Wiggle 2 Text: (singt wie Kind, schief aber süß) Was aufbau'n, was aufbau'n.... Eine Brücke hier, eine Brücke da, Fünfzehn-Stämme, trallalla... Jojojo...

sq_wait none
do_text 701d 1 {} Was
do_action anim talkrenga 0
sq_wait all
do_action skipp SummonPilzStammPos 1

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

sq_wait none
#Wiggle 1 action: sieht ihm nach, bis er aus dem Bild ist, zeigt ihm ein "biste wischi-waschi" nach. (So Hände vorm Kopf hin und her...)
do_action rotate SummonPilzStammPos 0
sq_actor actionlist 0 {{anim stretch} {anim standloopb} {anim standloopb} {rotate front} {anim pressupstart} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressuploop} {anim pressupstop}}
do_action anim getcomfort 0

#Pilzstamm erzeugen und aufnehmen
+sq_object summon Pilzstamm SummonPilzStammPos
link_obj [Object 1] [Object 0] 1
do_wait time 6

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

#Wiggle 2 action: kommt rein mit Pilzstamm in der Hand
do_action strike WiggleRightDropZone 1
do_wait time 10.5

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

#Wiggle 2 action: legt Stamm ab
do_action rotate back 1
do_wait time 0.3
do_action anim bend 1
link_obj [Object 1]
+sq_object beam 1 WiggleRightDropZone
do_wait time 1

do_action rotate right 1
do_wait time 0.5
do_text 701e 1 showup Eins
#Wiggle 2 action: geht wieder raus

do_action walk SummonPilzStammPos 1
do_wait time 6
do_change muetze wood 1 ab noanim
do_wait time 1
+sq_object delete 0

#+sq_oject beam [Object 1] SummonPilzStammPos???

do_action anim standloopa 0
do_wait time 1
#+sq_camera get
#Szene Ende


#701a Zehn Pilzstämme.
#701b Hmm.. 15 Stämme - sicher ist sicher.
#701c Jooo! Okay!
#701d Was aufbau'n, was aufbau'n.... Eine Brücke hier, eine Brücke da, Fünfzehn-Stämme, trallalla... Jojojo...


#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow metall
+adaptive_sound marker metall [get_pos this]
#-----------------------------------------

