#Clip 40c (nicht im Drehbuch) - Zwangsumsiedlung, Tor schließt sich
sq_text file Urwald
sq_audio open urw_040_c

sq_wait all
start_fade 1.0 0; #Licht aus

 +sq_pen set Tor [Getobjpos Riesentor 0]
 +sq_pen set imTor Tor
 +sq_pen move imTor {0.5 0 -5}
 +sq_pen set EndeKiste imTor
 +sq_pen move EndeKiste {1.5 0 0}
 +sq_pen set hinterTorfluegel Tor
 +sq_pen move hinterTorfluegel {-2 0 -4}

 set_anim [Getobjref Riesentor 0] riesentor.offen 0 0; #noch ist es offen
 call_method [Getobjref Riesentor 0] set_open

 sq_color 0 Wiggle1

 +sq_object summon Feuerstelle Tor
 +call_method [Object 0] packtobox
 +sq_actor find Feuerstelle 5 1 any Tor;#das brauchen wir fürs "Abbruchbeamen" am Schluß
 link_obj [Object 0] [Actor 0] 0; #jetzt hat er eine Kiste in der Hand

 sq_pen set anpassungAnSprecher 0
 sq_pen move anpassungAnSprecher {-5 0 0}
 do_action beam anpassungAnSprecher 0

 #do_change muetze transport 0 auf noanim
 sq_actor express 0 bad_normal
 do_wait time 1

start_fade 1.0 1; #wieder an und action

#sq_camera follow 0 1.5 -0.4 0.5
sq_camera fix 0 1.5 -0.4 0.5
sq_wait none
do_action transport imTor 0 ;#hinterTorfluegel 0; #und macht sich auf zum Tor
sq_camera move imTor 1.5 -0.4 0.5 0.03
do_wait time 2             ; #nach drei Sekunden fängt er an zu schimpfen
do_text 040_ca 0 {NoAnim} Ohdiese
do_action transport imTor 0 ;#hinterTorfluegel 0
#"Oh, diese..."
do_wait time 6.5
do_text 040_cb 0 {NoAnim} Rennenalle
do_action transport imTor 0
#do_action transport hinterTorfluegel 0
#"...rennen alle..."
do_wait time 4.5
do_text 040_cc 0 {NoAnim} Ihreausruestung
do_action transport imTor 0
#do_action transport hinterTorfluegel 0
#"...unsere Ausrüstung..."
do_wait time 7
sq_wait all                ;#jetzt wieder Stapelverarbeitung
+sq_camera fix imTor 0.9 -0.2 -0.7
#+sq_camera get
do_action transport imTor 0
do_action rotate right 0

#da kracht sie zu
   sq_pen set mitte Tor
   sq_pen move mitte {0.7 -2 -4}
   sq_pen set oben mitte
   sq_pen move oben {0 -1.4 0}
   sq_pen set rechtsoben mitte
   sq_pen move rechtsoben {0 -1 1}
   sq_pen set rechts mitte
   sq_pen move rechts {0 0 1.4}
   sq_pen set rechtsunten mitte
   sq_pen move rechtsunten {0 1 1}
   sq_pen set unten mitte
   sq_pen move unten {0 1.4 0}
   sq_pen set linksoben mitte
   sq_pen move linksoben {0 -1 -1}
   sq_pen set links mitte
   sq_pen move links {0 0 -1.4}
   sq_pen set linksunten mitte
   sq_pen move linksunten {0 1 -1}
set_anim [Getobjref Riesentor 0] riesentor.schliessen 0 1

sq_wait none
do_action anim standbackhith 0
   do_particle create 13 oben {0 0 0} 23 1
   do_particle create 13 linksoben {0 0 0.01} 23 1
   do_particle create 13 links {0 -0.01 0.01} 23 1
   do_particle create 13 linksunten {0 -0.05 0.01} 23 1
   do_particle create 13 unten {0 -0.05 0} 23 1
   do_particle create 13 rechtsunten {0 -0.05 -0.01} 23 1
   do_particle create 13 rechts {0 -0.01 -0.01} 23 1
   do_particle create 13 rechtsoben {0 0 -0.01} 23 1
   do_particle create 13 oben {0 0 0} 23 1
   do_wait time 0.4
+link_obj [Object 0]
+do_action beam EndeKiste 1
+set_anim [Getobjref Riesentor 0] riesentor.zu 0 0
+call_method [Getobjref Riesentor 0] set_closed
sq_pen set wegvomTor imTor
sq_pen move wegvomTor {0.5 0 -0.5}
do_wait time 1.5
sq_wait 0
do_action walk wegvomTor 0

sq_wait none
+do_action beam wegvomTor 0
sq_camera move 0 0.9 -0.2 -0.8
do_wait time 2
+adaptive_sound changethemenow metall
+adaptive_sound marker metall [parse_pos Tor] 1000

