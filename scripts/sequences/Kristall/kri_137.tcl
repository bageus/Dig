#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s137
#-----------------------------------------
sq_pen set MitteGalerie TriggerPos
sq_pen move MitteGalerie {11 0 0}
sq_pen set LinkeTuer MitteGalerie
sq_pen move LinkeTuer {-14 0 4}
sq_pen set RechteTuer MitteGalerie
sq_pen move RechteTuer {14 0 4}

do_wait time 1
sq_camera fix LinkeTuer 1.4 -0.3 -0.6
do_wait time 0.5
+opendoor left
do_wait time 1.5
sq_camera fix RechteTuer 1.4 -0.3 0.6
do_wait time 1
+opendoor right
do_wait time 1
+sq_camera fix MitteGalerie 0.8 -0.1 0.0
do_wait time 1
sq_camera move MitteGalerie 1.2 -0.1 0.0
do_wait time 2
+sq_camera get
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow kristall
#-----------------------------------------

