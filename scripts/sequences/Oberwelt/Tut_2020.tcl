sq_text file Tutorial
sq_audio open 2020
+do_elf hide
+sq_actor find Zwerg 200 4 0
sq_actor find Feuerstelle 200 1 0

sq_wait none
#sq_pen set FeuerPos [Getobjpos Feuerstelle]

#Diese Zeile ist der Anlass fuer das Nichtfunktionieren des Bugfixes 1250!!!!!!!!!!!!!!!!!!
if {[get_objname [Actor 2]]=="Ole"} {global actors; set [Actor 2] [lindex $actors 3];set [Actor 3] [lindex $actors 2]} else {log "Nicht getauscht!!!"}
#!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


sq_pen set FeuerPos 4

sq_pen set Z1Pos FeuerPos
sq_pen move Z1Pos {-0.5 0 2}
sq_pen set Z2Pos FeuerPos
sq_pen move Z2Pos {1 0 -1}

sq_pen set Z3Pos FeuerPos
sq_pen move Z3Pos {0.8 0 2}
sq_pen set Z4Pos FeuerPos
sq_pen move Z4Pos {2 0 2.5}

sq_pen set Z3BPos FeuerPos
sq_pen move Z3BPos {15 0 0}
sq_pen set Z4BPos FeuerPos
sq_pen move Z4BPos {15 0 0}

sq_pen set CamPos FeuerPos

sq_camera fix CamPos 1.0 -0.2 -0.2

do_action walk Z1Pos 2
do_action walk Z2Pos 3

do_action beam Z3BPos 0
do_action beam Z4BPos 1

do_wait time 4
sq_actor actionlist 2 { {anim scratchhead} {anim teeter_w} {anim wait} {anim stretch} loop}
sq_actor actionlist 3 { {anim stretch} {anim wait} {anim teeter_w} loop}
do_wait time 2
do_action run Z3Pos 0
do_wait time 0.4
do_action run Z4Pos 1
do_wait time 2

sq_camera fix 0 1.2 -0.3 -0.4
do_wait time 2
sq_camera fix CamPos 1.0 -0.2 -0.2

do_action rotate front 3
do_wait time 1
do_action rotate right 2
do_wait time 5

sq_actor actionlist 0 {}
sq_actor actionlist 1 {}
sq_actor actionlist 2 {}
sq_actor actionlist 3 {}

do_wait time 1
#sq_wait all

do_action anim breathe 0
do_wait time 1.5
do_text 2020a 0 {talkk talka} Hat_der
do_wait time 3
do_text 2020b 2 {talkb talkp talkm} Nein_wir
do_wait time 3
sq_pen move Z4Pos {-2.0 0 1}
do_action walk Z4Pos 1
do_wait time 2
do_action rotate 1.9 1
do_text 2020c 1 {talkk talka} Vergiss_es
do_wait time 3
sq_wait all
do_wait camera
sq_camera move FeuerPos 1.2 -0.4 0.1 0.7
do_wait time 2
