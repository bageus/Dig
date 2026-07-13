#Clip 121 - Krake stirbt durch Kronleuchter
#"Krake_" ist das Objekt, das die Animationen durchführt, "Krake" ist Angreifbares

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------

#+set krake [obj_query this -class Krake]; del $krake
#hier wird das angreifbare Dummyobjekt gelöscht

+sq_actor find Krake_
#der neue Actor 1
sq_wait 0
sq_camera selset inout

sq_pen set fall_unten 1;#[Getobjref Krake_ 0]
sq_pen set fall_oben fall_unten
sq_pen move fall_oben {-2 -3 0}
sq_pen set zoomout fall_unten
sq_pen move zoomout {-4 -3 0}
sq_pen move fall_unten {-1 -1.5 1}

sq_camera fix 0 0.75 -0.3 -0.3
do_action rotate 1 0

  do_wait time 0.5
  sq_camera move zoomout 1.5 -0.11 0.32 0.7
  do_wait time 1
  sound play zahnrad_a_loop 1
  do_wait time 2
  sq_camera fix fall_oben 1.1 -0.6 -0.15;#1.25 -0.68 -0.14 ;#;1.1 -0.6 -0.15
  #sq_wait camera
  #sq_camera move fall_oben 1 -0.6 0 0.5
  #do_wait time 3

+call_method /obj/Krake_real kill_switch
  #+set_anim [Getobjref Krake_ 0] krake.tod_durch_kronleuchter 0 1
  #+do_action anim krake.tod_durch_kronleuchter 1
do_wait time 2.8
sq_camera move fall_unten 1.35 0.08 -0.45 0.6;#1.6 -0.23 -0.23 0.6
do_wait time 1.3
sound play fe_schritt1 1
sq_screenvibe kawumm
do_wait time 4.7
sq_camera move fall_unten 1.6 0.08 -0.45 0.25
do_wait time 5
+sq_camera get

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow titanic
#-----------------------------------------

