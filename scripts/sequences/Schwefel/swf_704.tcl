log "Bruecke 4"

start_fade 0.5 0
do_wait time 0.5
adaptive_sound changethemenow atmoschwefel

+sq_pen set Bruecke [Getobjpos Schwefelbruecke]
+sq_pen set Cam01 Bruecke
+sq_pen set Cam02 Bruecke
+sq_pen move Cam02 { 4 -0.1 2 }
+sq_pen set Cam03 Bruecke
+sq_pen move Cam03 { -6 -2 2 }
+sq_pen set Cam04 Bruecke
+sq_pen move Cam04 { 5 -0.2 2 }
+sq_pen set Cam05 Bruecke
+sq_pen move Cam05 { -6 -0.2 2 }
+sq_pen set Cam06 Bruecke
+sq_pen move Cam06 { -6.5 0.3 2 }
+sq_pen set Cam07 Bruecke
+sq_pen move Cam07 { 14 0.4 0 }
+sq_pen set Cam08 Bruecke
+sq_pen move Cam08 { 14 0.4 0 }

+sq_camera selset inout

change_particlesource [Getobjref Schwefelbruecke] 1 18 { -7.2 -0.1 0 } { 0 -0.05 0 } 100 5 0 0
change_particlesource [Getobjref Schwefelbruecke] 2 18 { -4 0.1 0 } { 0 -0.05 0 } 100 5 0 0


change_particlesource [Getobjref Schwefelbruecke] 3 18 { -3 0.1 0 } { 0 -0.05 0 } 100 5 0 0
change_particlesource [Getobjref Schwefelbruecke] 4 18 { -4.8 0 0 } { 0 -0.05 0 } 100 5 0 0

+sq_pen set RunStartPos Bruecke
+sq_pen set RunEndPos Bruecke

+sq_pen move RunStartPos {-15 -0.5 -2}
+sq_pen move RunEndPos {13.5 -0.5 -4}
+sq_pen set DropPos RunEndPos
+sq_pen move DropPos {0.4 0.4 0}
#Die zwei Zeilen nur um das ganze zu Testen
+set_anim [Getobjref Schwefelbruecke] swf_bruecke.ganz 0 0
+call_method [Getobjref Schwefelbruecke] set_repaired


#Ein Wiggle mit ner Kiste in der Hand.
#+sq_object summon Zwerg
#do_wait
set_autolight [Actor 0] 0
#sq_actor find Zwerg
#do_wait
do_action beam RunStartPos 0

+sq_object summon Halbzeug_kiste 0
+do_wait

+call_method [Object 0] change_look tragen
link_obj [Object 0] [Actor 0] 0
do_wait time 0.3

do_action transport RunEndPos 0
sq_camera fix Cam01 2.2 -0.2 0
do_wait time 2
start_fade 4 1
do_wait time 5
sq_camera move Cam05 1.1 -0.15 -0.6 0.3
do_wait time 6.7
sq_camera move Cam06 0.8 -0.15 -0.6 1
screenvibe 10 14 0 0.01 103 0.01 200
do_wait time 0.3
set_particlesource [Getobjref Schwefelbruecke] 1 1
do_wait time 0.2
change_particlesource [Getobjref Schwefelbruecke] 1 12 { -7.2 -0.2 0 } { 0 0 0 } 100 1 0 0
do_wait time 0.2
+set_particlesource [Getobjref Schwefelbruecke] 1 0
do_wait time 0.6
sq_camera move Cam06 1.2 -0.15 -0.6 0.2
do_wait time 2
sound play bruecke_crash 1
do_wait time 1
sq_pen set CamTemp 0
sq_pen move CamTemp { 1 0 0 }
sq_camera fix CamTemp 1 -0.2 0.3 0.2
do_wait time 1
sq_pen move CamTemp { 0 0.4 0 }
#sq_camera move CamTemp 0.8 -0.15 0.3 1
do_wait time 0.3
set_particlesource [Getobjref Schwefelbruecke] 2 1
do_wait time 0.2
change_particlesource [Getobjref Schwefelbruecke] 2 12 { -4 0.1 0 } { 0 0 0 } 100 1 0 0
do_wait time 0.2
+set_particlesource [Getobjref Schwefelbruecke] 2 0
do_wait time 1.3
sq_camera move Cam03 1.6 -0.3 0.95 0.2
set_particlesource [Getobjref Schwefelbruecke] 3 1
do_wait time 0.2
change_particlesource [Getobjref Schwefelbruecke] 3 12 { -3 0.1 0 } { 0 0 0 } 100 1 0 0
do_wait time 0.2
+set_particlesource [Getobjref Schwefelbruecke] 3 0
do_wait time 1.3
set_particlesource [Getobjref Schwefelbruecke] 4 1
do_wait time 0.2
change_particlesource [Getobjref Schwefelbruecke] 4 12 { -4.8 0 0 } { 0 0 0 } 100 1 0 0
do_wait time 0.2
+set_particlesource [Getobjref Schwefelbruecke] 4 0
do_wait time 1.3

do_wait time 6
do_wait time 0.5
do_wait time 2
+set_anim [Getobjref Schwefelbruecke] swf_bruecke.einsturz 0 1
do_wait time 2
sound play fe_schritt1 1
sq_screenvibe kawumm
sq_camera move Cam04 1.2 -0.2 0.95 0.2
do_wait time 2
sound play fe_schritt2 0.5
sq_screenvibe steps
do_wait time 0.7
sound play fe_schritt1 1
sq_screenvibe kawumm
do_wait time 1.2
sound play fe_schritt2 0.5
sq_screenvibe steps
do_wait time 0.5
sound play fe_schritt2 0.5
sq_screenvibe steps
do_wait time 0.3
sound play fe_schritt1 1
sq_screenvibe kawumm
do_wait time 1
sq_screenvibe steps
do_wait time 1

sq_camera follow 0 1.2
do_wait time 4.2
sq_camera move Cam07 1.0 -0.3 0.6

do_wait time 2
sq_wait 0
+set_anim [Getobjref Schwefelbruecke] swf_bruecke.einsturz 149 1
do_wait
sq_camera move 0 0.65 0 -0.9 0.3
+link_obj [Object 0]
+sq_object beam 0 RunStartPos
do_wait
+sq_object delete 0

+do_wait
sq_actor eyes 0 {c c c c c c c 9 9 9 9 9 c 9 c c l l l c r r r c c c c c c 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 c}
sq_actor actionlist 0 { {anim putboxb} {anim tired} {wait 2} {rotate front} {rotate left} { wait 1 } { anim shock } {wait 0.5} { anim tired } }
+sq_object summon Feuerstelle RunStartPos
do_wait
+call_method [Object 0] packtobox
+set_pos [Object 0] [parse_pos DropPos]
do_action anim putboxa 0
do_wait time 1
sq_camera move Cam01 2 -0.2 -0.9 0.1
do_wait time 7
start_fade 2 0
+sq_camera get
do_wait time 3

+call_method [Getobjref Schwefelbruecke] set_destroyed
+set_autolight [Actor 0] 1
+set_pos [Actor 0] [parse_pos RunEndPos]
+cancel_fade
+adaptive_sound changethemenow kristall
+adaptive_sound marker kristall [parse_pos DropPos] 1000

do_wait


