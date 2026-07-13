sq_audio open 0092
sq_text file Schwefel

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow schwefelseen
#-----------------------------------------

+sq_actor find Drache
+sq_color 1 red
sq_wait camera
sq_wait all



+sq_pen set DrachenPos TriggerPos
+sq_pen move DrachenPos {14 0 0}

+sq_pen set MusicPos TriggerPos
+sq_pen move MusicPos {20 0 0}

+sq_pen set WigglePos TriggerPos
+sq_pen move WigglePos {-1 0 0}

+sq_pen set WiggleCamPos WigglePos
+sq_pen move WiggleCamPos {0 0 -2}

+sq_pen set BackBeamPos TriggerPos
+sq_pen move BackBeamPos {-2 0 0}

+sq_pen move WiggleCamPos {-0.5 0 0}
sq_camera fix WiggleCamPos 0.8 -0.2 0.5
sq_wait none
sq_actor eyes 0 {0}
+do_action walk TriggerPos 0
do_wait time 6.0
+sq_pen move WiggleCamPos {0.5 0 0}
sq_camera fix WiggleCamPos 0.8 -0.2 -1.0
sq_wait all
+do_action walk TriggerPos 0
+do_action rotate 4.71 0
do_action anim shock 0
do_text 092a 0 Auto Ohoh
do_wait time 0.5
sq_actor eyes 0 {c c 9 c c c c 9 c c}
do_wait time 1
#sq_camera move DrachenPos 1.3 -0.5 0.4 0.5
sq_wait none
sq_wait 0
#sq_camera fix WiggleCamPos 0.8 -0.2 -1.0
do_action anim mann.gehen_zurueck 0
do_wait time 0.5
sq_camera move DrachenPos 1.3 -0.5 0.4 0.3
do_wait time 4
sq_camera move DrachenPos 1.4 -0.6 0.3
do_wait time 2
+do_action beam 0 BackBeamPos
+set_pos [Actor 0] [parse_pos BackBeamPos]
sq_camera get
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker schwefelseen [parse_pos MusicPos]
#-----------------------------------------


