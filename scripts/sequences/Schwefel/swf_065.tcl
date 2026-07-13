sq_text file Schwefel
+sq_actor find Drache
+sq_color 1 red

do_wait camera
sq_wait all

#################Pens#########################

+sq_pen set DrachenCamPos 1
+sq_pen move DrachenCamPos {20 -2 0}

#############################################

sq_camera move DrachenCamPos 1.5 0.1 0.1
do_wait time 4

