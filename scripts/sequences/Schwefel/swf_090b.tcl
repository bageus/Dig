+set_owner [obj_query 0 -class TitanicPumpe] 0
+set_hoverable [obj_query 0 -class TitanicPumpe] 1
+set_selectable [obj_query 0 -class TitanicPumpe] 1
+set_objname [obj_query 0 -class TitanicPumpe] "Titanic Pumpe"
sq_audio open 0090B

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------

sq_text file Schwefel

sq_wait camera
+sq_pen set OfenPos TriggerPos
+sq_pen move OfenPos {0 -1.5 0}

+sq_pen set Music1Pos TriggerPos
+sq_pen move Music1Pos {26.5 -1 0}

+sq_pen set Music2Pos TriggerPos
+sq_pen move Music2Pos {67.75 -13 0}

+sq_pen set Music3Pos TriggerPos
+sq_pen move Music3Pos {61.65 3 0}

+sq_pen set Music4Pos TriggerPos
+sq_pen move Music4Pos {115.75 -9 0}

+sq_pen set WigglePos TriggerPos
+sq_pen move WigglePos {-1 0 2}

+sq_pen set StartPos TriggerPos
+sq_pen move StartPos {-5.5 0 8}

+sq_pen set WalkInPos StartPos
+sq_pen move WalkInPos {3 0 0}

+sq_pen set PreWalkInPos WalkInPos
+sq_pen move PreWalkInPos {-1.5 0 0}

sq_wait all
#sq_camera move OfenPos 1.0 0 0 0.6
#do_wait time 3

+sq_pen move OfenPos {6 0 0}
#sq_camera move OfenPos 1.0 0 0 0.3
#do_wait time 1
+sq_pen move OfenPos {-2 -0.5 0}
#sq_camera move OfenPos 1.2 0 -0.5 0.3
#do_wait time 5

+sq_pen set FirstPos TriggerPos
+sq_pen move FirstPos {0 -1.3 0}


sq_camera fix FirstPos 1.0 0.2 0.3
+do_action beam PreWalkInPos 0
+do_action rotate TriggerPos 0
do_wait time 1
#sq_camera fix FirstPos 1.5 0 0
do_wait time 2
+do_action walk WalkInPos 0
+do_action rotate TriggerPos 0
do_wait time 1

do_action anim scratchhead 0
#do_text "???" 0
#do_text 090ba 0 Auto Hmm_a
do_text 090bb 0 Auto Was_ist
do_wait time 1
+do_action walk WigglePos 0
+sq_pen move OfenPos {-4.5 1.4 1}
sq_camera fix OfenPos 0.8 0.19 -0.65
do_wait time 2
#do_text "Wasn das fürn Ding ?" 0
do_wait time 1
#do_text "Ich glaube es braucht so eine Art Brennstoff ..." 0
do_text 090bc 0 Auto Sieht_aus
sq_camera move OfenPos 0.8 0.19 -0.85 0.1
do_wait time 2
do_text 090bd 0 Auto Ach_was
#do_text "Ach, was weiss ich ..." 0
sq_camera move OfenPos 1.0 -0.19 -0.85
#sq_camera move 0 1.2 -0.3 0.3
do_wait time 2

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker titanic [parse_pos Music1Pos] 20
+adaptive_sound marker titanic [parse_pos Music2Pos] 20
+adaptive_sound marker titanic [parse_pos Music3Pos] 50
+adaptive_sound marker titanic [parse_pos Music4Pos] 40
#-----------------------------------------

