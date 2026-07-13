#--- Clip 040b --- Wiggle kommt mit Ring der Natur zur Torwächterin ---
sq_text file Urwald
sq_audio open urw_040_b

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow twopen
#-----------------------------------------

+sq_actor find Zwerg 10 1 0
+sq_actor find Torwaechterin
+set_visibility [Actor 1] 1
+sq_pen set SchPlTor 1
#+sq_pen move SchPlTor {-0.5 0 0}
sq_camera selset inout

#--- Positionen festlegen ---
+sq_pen set Tor [Getobjpos Riesentor 0]
+sq_pen move Tor {-2 0 -4}
+sq_pen set zaubern [Getobjpos Riesentor 0]
+sq_pen move zaubern {-4 0 -4}
+sq_pen set oeffnenCam2 [Getobjpos Riesentor 0]
+sq_pen move oeffnenCam2 {0 -1 -10}
+sq_pen set erstesRuckelnKamera [Getobjpos Riesentor 0]
+sq_pen move erstesRuckelnKamera {0 -1 -5}

+sq_pen set sPosZ 0
+sq_pen set bePosZ 0
sq_pen set sPosT Tor
sq_pen setz sPosT 15
sq_color 0 Wiggle1
sq_color 1 Voodoo1


#--- Torwächterin wartet ---
sq_wait 1
do_action beam sPosT 1
do_action rotate front 1
sq_wait none
sq_actor actionlist 1 {loopstart {anim sitedgeloopa} {anim sitedgeloopb} loop}
do_action anim sitedgeloopa 1


#--- Wiggle läuft los zum Tor
sq_pen set cPos01 Tor
sq_pen move cPos01 {-16 0 0}
sq_pen setz bePosZ 11
sq_pen move bePosZ {8 0 0}
do_action walk bePosZ 0
sq_wait none

sq_pen set cPos01 Tor
sq_pen move cPos01 {-1 0 0}
sq_pen setz cPos01 16
sq_camera fix cPos01 1.2 0.0 0.0

sq_pen setz bePosZ 8.5
sq_pen move bePosZ {10 0 0}
do_action beam bePosZ 0

sq_pen set bePosZ zaubern
sq_pen move bePosZ {-1.5 0 0}
sq_pen set zwischen_den_beiden zaubern
sq_pen move zwischen_den_beiden {-0.75 0 0}

sq_actor actionlist 1 {}

#--- Gespräch am Tor und übergabe Ring ---
sq_wait 1
do_action walk bePosZ 0
do_action anim standup_edge 1
sq_camera move zwischen_den_beiden 0.8 0 0.3 0.1
do_action walk zaubern 1
do_action rotate right 0
do_action rotate left 1

sq_wait 0
sq_object summon Ring_Des_Lebens
call_method [Object 0] setstandardanim
link_obj [Object 0] [Actor 0] 0

sq_wait none
do_text 040b_a 0 {Auto} Hierbitte ;#Hier, bitte
do_wait time 1
do_action anim offerjoint 0
do_action anim offerjoint 1
do_wait time 1
link_obj [Object 0] [Actor 1] 0

sq_wait 1
do_text 040b_b 1 {Auto} Dannwerd;#Dann werd

#--- Vorbereitung ---
do_action rotate right 1
do_action rotate left 1
do_text 040b_c 1 {Auto} Rutschmal;#Rutsch mal

sq_pen move bePosZ {-1.5 0 0}
sq_pen move zwischen_den_beiden {-1.25 0 0}
sq_pen move zaubern {-1 0 0}
sq_wait 1
sq_camera fix zaubern 0.65 -0.1 -0.6
do_text 040b_d 0 {Auto} Nagut;#Na gut.
do_wait time 0.1
do_action walk bePosZ 0
do_action walk zaubern 1

sq_wait none
do_action rotate right 1
do_action rotate right 0
do_wait time 0.8
do_text 040b_e 1 {Auto} Danke;#Danke...
sq_actor eyes 1 {c 12 12 13 13 c c}
do_wait time 1
sq_camera fix zaubern 0.8 -0.2 0.7
do_wait time 0.5
do_text 040b_f 1 {{breathe}} Wollenwir
sq_wait camera
sq_camera move oeffnenCam2 1.1 -0.2 0.7 0.1

#-----Zauberey-----
sq_wait none
sq_actor actionlist 1 {loopstart {anim magicloop} loop}

   #vorbereitende Partikeleffekte
   sq_pen set amRing 1
   sq_pen move amRing {0 -1.1 0.5}
   sq_pen set mitte Tor
   sq_pen move mitte {0.5 -1 -1.5}
   sq_pen set oben mitte
   sq_pen move oben {0 -0.8 0}
   sq_pen set rechtsoben mitte
   sq_pen move rechtsoben {0 -0.5 0.5}
   sq_pen set rechts mitte
   sq_pen move rechts {0 0 0.8}
   sq_pen set rechtsunten mitte
   sq_pen move rechtsunten {0 0.5 0.5}
   sq_pen set unten mitte
   sq_pen move unten {0 0.8 0}
   sq_pen set linksoben mitte
   sq_pen move linksoben {0 -0.5 -0.5}
   sq_pen set links mitte
   sq_pen move links {0 0 -0.8}
   sq_pen set linksunten mitte
   sq_pen move linksunten {0 0.5 -0.5}

   do_particle create 13 amRing {0 -0.01 0} 5 1
   do_particle create 13 rechtsoben {0 0 -0.01} 5 1
   do_wait time 0.2
   do_particle create 13 unten {0 -0.05 0} 5 1
   do_wait time 0.3
   do_particle create 13 linksoben {0 0 0.01} 5 1
   do_wait time 0.1

sq_actor actionlist 1 {loopstart {anim magicloop} loop}
do_action anim magicstart 1
   do_particle create 13 1 {0 -0.1 0} 125 3
   do_wait time 1
   do_particle create 13 oben {0 0 0} 23 1
   do_particle create 13 rechtsoben {0 0 -0.01} 23 1
   do_particle create 13 rechts {0 -0.01 -0.01} 23 1
   do_particle create 13 rechtsunten {0 -0.05 -0.01} 23 1
   do_particle create 13 unten {0 -0.05 0} 23 1
   do_particle create 13 linksunten {0 -0.05 0.01} 23 1
   do_particle create 13 links {0 -0.01 0.01} 23 1
   do_particle create 13 linksoben {0 0 0.01} 23 1
   do_particle create 13 oben {0 0 0} 23 1

sq_wait camera
do_wait time 1.5
screenvibe 1 1.5 1 0.1 220 0.1 300                                ;#vorsichtiges Anfangsruckeln
do_wait time 1
sq_camera fix erstesRuckelnKamera 0.9 0 0.8
do_wait time 1
do_particle create 13 rechtsoben {0 0 -0.01} 23 1
do_wait time 1
do_particle create 13 linksunten {0 -0.05 0.01} 23 1
do_wait time 1

#sq_wait camera
sq_camera fix 1 0.7 -0.6 -0.5
sq_camera selset standard
screenvibe 12 3 2 0.2 220 0.2 300                               ;#fettes Erdbeben
sq_actor actionlist 1 {loopstart {anim magicloop} loop}
do_action anim magicloop 1
do_text 040b_g 1 {NoAnim} Nimmden
sq_actor actionlist 1 {loopstart {anim magicloop} loop}
do_action anim magicloop 1

   sq_pen set mittewaechterin 1
   sq_pen move mittewaechterin {0 -0.7 0}
   sq_pen set up mittewaechterin
   sq_pen move up {0 -1 0}
   sq_pen set do mittewaechterin
   sq_pen move do {0 0.8 0}
   sq_pen set le mittewaechterin
   sq_pen move le {0 0 -1}
   sq_pen set ri mittewaechterin
   sq_pen move ri {0 0 1}
   do_wait time 0.5
   do_particle create 13 up {0 0.08 0} 23 2
   do_particle create 13 do {0 -0.08 0} 23 2
   do_particle create 13 ri {0 0 0.08} 23 2
   do_particle create 13 le {0 0 -0.08} 23 2
   do_action anim protecteyes 0
   do_wait time 0.5
   do_particle create 13 up {0 0.09 0} 58 1.5
   do_particle create 13 do {0 -0.09 0} 58 1.5
   do_particle create 13 ri {0 0 0.09} 58 1.5
   do_particle create 13 le {0 0 -0.09} 58 1.5
   do_wait time 0.5
   do_particle create 13 up {0 0.1 0} 93 1
   do_particle create 13 do {0 -0.1 0} 93 1
   do_particle create 13 ri {0 0 0.1} 93 1
   do_particle create 13 le {0 0 -0.1} 93 1
   do_wait time 0.5

sq_camera move 1 0.7 -0.3 -0.7
do_wait time 0.3
do_particle create 13 mittewaechterin {0 -0.1 0.05} 125 0.5
do_wait time 0.2
sq_camera move 1 0.7 0 -0.9
do_wait time 0.2
do_particle create 13 amRing {0.2 -0.05 -0.1} 125 6
do_particle create 13 amRing {0 -0.1 0} 58 3
do_wait time 0.1
do_particle create 13 amRing {0.2 -0.05 -0.1} 125 6
do_wait time 0.1
do_particle create 13 amRing {0.2 -0.05 -0.1} 125 6
do_wait time 0.1
do_particle create 13 amRing {0.2 -0.05 -0.1} 125 6
do_wait time 0.1
do_particle create 13 amRing {0.2 -0.05 -0.1} 125 6
do_wait time 0.1
do_particle create 13 amRing {0.2 -0.05 -0.1} 125 6
do_wait time 0.1

sq_camera selset inout

#mehr Partikeleffekte?

sq_pen move erstesRuckelnKamera {-1.5 0 0}
sq_camera fix erstesRuckelnKamera 1 0.1 0.2
do_wait time 0.5
set_anim [Getobjref Riesentor 0] riesentor.efeu 0 1
do_particle create 13 unten {0 -0.1 0} 23 1
do_wait time 2
do_particle create 13 oben {0 -0.05 0} 23 1
do_wait time 1

sq_camera fix oeffnenCam2 1.0 -0.05 0.7
   #Partikeleffekte
   sq_pen set schloss Tor
   sq_pen move schloss {1 -0.5 -0.5}
   sq_pen set T1 schloss
   sq_pen move T1 {0 -0.5 0}
   sq_pen set T2 T1
   sq_pen move T2 {0 -0.5 0}
   sq_pen set T3 T2
   sq_pen move T3 {0 -0.5 0}
   sq_pen set T4 T3
   sq_pen move T4 {0 -0.5 0}
   sq_pen set T5 T4
   sq_pen move T5 {0 -0.5 0};#(das war) aufwärts
   sq_pen set T6 T5
   sq_pen move T6 {0 0 0.5}
   sq_pen set T6b T5
   sq_pen move T6b {0 0 -0.5}
   sq_pen set T7 T6
   sq_pen move T7 {0 0 0.5}
   sq_pen set T7b T6b
   sq_pen move T7b {0 0 -0.5}
   sq_pen set T8 T7
   sq_pen move T8 {0 0 0.5}
   sq_pen set T8b T7b
   sq_pen move T8b {0 0 -0.5}
   sq_pen set T9 T8
   sq_pen move T9 {0 0 0.5}
   sq_pen set T9b T8b
   sq_pen move T9b {0 0 -0.5}
   sq_pen set T10 T9
   sq_pen move T10 {0 0 0.5}
   sq_pen set T10b T9b
   sq_pen move T10b {0 0 -0.5}
   sq_pen set T10c T10
   sq_pen move T10c {0 0 0.5}
   sq_pen set T10d T10b
   sq_pen move T10d {0 0 -0.5}
   sq_pen set T10e T10c
   sq_pen move T10e {0 0 0.5}
   sq_pen set T10f T10d
   sq_pen move T10f {0 0 -0.5}
   sq_pen set T10g T10e
   sq_pen move T10g {0 0 0.5}
   sq_pen set T10h T10f
   sq_pen move T10h {0 0 -0.5};#oben seitwärts
   sq_pen set T11 T10g
   sq_pen move T11 {0 0.5 0}
   sq_pen set T11b T10h
   sq_pen move T11b {0 0.5 0}
   sq_pen set T12 T11
   sq_pen move T12 {0 0.5 0}
   sq_pen set T12b T11b
   sq_pen move T12b {0 0.5 0}
   sq_pen set T13 T12
   sq_pen move T13 {0 0.5 0}
   sq_pen set T13b T12b
   sq_pen move T13b {0 0.5 0}
   sq_pen set T14 T13
   sq_pen move T14 {0 0.5 0}
   sq_pen set T14b T13b
   sq_pen move T14b {0 0.5 0}
   sq_pen set T15 T14
   sq_pen move T15 {0 0.5 0}
   sq_pen set T15b T14b
   sq_pen move T15b {0 0.5 0};#rechts abwärts
   sq_pen set T16 T15
   sq_pen move T16 {0 0 -0.5}
   sq_pen set T16b T15b
   sq_pen move T16b {0 0 0.5}
   sq_pen set T17 T16
   sq_pen move T17 {0 0 -0.5}
   sq_pen set T17b T16b
   sq_pen move T17b {0 0 0.5}
   sq_pen set T18 T17
   sq_pen move T18 {0 0 -0.5}
   sq_pen set T18b T17b
   sq_pen move T18b {0 0 0.5}
   sq_pen set T19 T18
   sq_pen move T19 {0 0 -0.5}
   sq_pen set T19b T18b
   sq_pen move T19b {0 0 0.5}
   sq_pen set T20 T19
   sq_pen move T20 {0 0 -0.5}
   sq_pen set T20b T19b
   sq_pen move T20b {0 0 0.5}
   sq_pen set T21 T20
   sq_pen move T21 {0 0 -0.5}
   sq_pen set T21b T20b
   sq_pen move T21b {0 0 0.5};#unten seitwärts
   sq_pen set T22 T21
   sq_pen move T22 {0 0 -1.5}
   sq_pen set T23 T22
   sq_pen move T23 {0 -0.5 0}


   do_wait time 0.5
   do_particle create 13 schloss {-0.15 -0.05 0} 93 2
   do_particle create 14 T1 {-0.1 -0.05 0} 3 2
   do_particle create 14 T2 {-0.1 -0.05 0} 3 2
   do_particle create 14 T3 {-0.1 -0.05 0} 3 2
   do_particle create 14 T4 {-0.1 -0.05 0} 3 2
   do_particle create 14 T5 {-0.1 -0.05 0} 3 2
   do_particle create 14 T6 {-0.1 -0.05 0} 3 2
   do_particle create 14 T6b {-0.1 -0.05 0} 3 2
   do_particle create 14 T7 {-0.1 -0.05 0} 3 2
   do_particle create 14 T7b {-0.1 -0.05 0} 3 2
   do_particle create 14 T8 {-0.1 -0.05 0} 3 2
   do_particle create 14 T8b {-0.1 -0.05 0} 3 2
   do_particle create 14 T9 {-0.1 -0.05 0} 3 2
   do_particle create 14 T9b {-0.1 -0.05 0} 3 2
   do_particle create 14 T10 {-0.1 -0.05 0} 3 2
   do_particle create 14 T10b {-0.1 -0.05 0} 3 2
   do_particle create 14 T10c {-0.1 -0.05 0} 3 2
   do_particle create 14 T10d {-0.1 -0.05 0} 3 2
   do_particle create 14 T10e {-0.1 -0.05 0} 3 2
   do_particle create 14 T10f {-0.1 -0.05 0} 3 2
   do_particle create 14 T10g {-0.1 -0.05 0} 3 2
   do_particle create 14 T10h {-0.1 -0.05 0} 3 2
   do_particle create 14 T11 {-0.1 -0.05 0} 3 2
   do_particle create 14 T11b {-0.1 -0.05 0} 3 2
   do_particle create 14 T12 {-0.1 -0.05 0} 3 2
   do_particle create 14 T12b {-0.1 -0.05 0} 3 2
   do_particle create 14 T13 {-0.1 -0.05 0} 3 2
   do_particle create 14 T13b {-0.1 -0.05 0} 3 2
   do_particle create 14 T14 {-0.1 -0.05 0} 3 2
   do_particle create 14 T14b {-0.1 -0.05 0} 3 2
   do_particle create 14 T15 {-0.1 -0.05 0} 3 2
   do_particle create 14 T15b {-0.1 -0.05 0} 3 2
   do_particle create 14 T16 {-0.1 -0.05 0} 3 2
   do_particle create 14 T16b {-0.1 -0.05 0} 3 2
   do_particle create 14 T17 {-0.1 -0.05 0} 3 2
   do_particle create 14 T17b {-0.1 -0.05 0} 3 2
   do_particle create 14 T18 {-0.1 -0.05 0} 3 2
   do_particle create 14 T18b {-0.1 -0.05 0} 3 2
   do_particle create 14 T19 {-0.1 -0.05 0} 3 2
   do_particle create 14 T19b {-0.1 -0.05 0} 3 2
   do_particle create 14 T20 {-0.1 -0.05 0} 3 2
   do_particle create 14 T20b {-0.1 -0.05 0} 3 2
   do_particle create 14 T21 {-0.1 -0.05 0} 3 2
   do_particle create 14 T21b {-0.1 -0.05 0} 3 2
   do_particle create 14 T22 {-0.1 -0.05 0} 3 2
   do_particle create 14 T23 {-0.1 -0.05 0} 3 2
do_wait time 1

   sq_pen move T1 {1.5 0.3 -0.2}
   sq_pen move T3 {1.5 0.3 -0.2}
   sq_pen move T4 {1.5 0.3 -0.2}
   sq_pen move schloss {1.5 0.3 -0.2}
   sq_pen move T5 {1.5 0.3 -0.2}
   sq_pen move T2 {1.5 0.3 -0.2}
   sq_pen move T23 {1.5 0.3 -0.2}
   do_particle create 14 T1 {-0.1 -0.05 0} 3 2
   do_particle create 14 T2 {-0.1 -0.05 0} 3 2
   do_particle create 14 T3 {-0.1 -0.05 0} 3 2
   do_particle create 14 T4 {-0.1 -0.05 0} 3 2
   do_particle create 14 T5 {-0.1 -0.05 0} 3 2
   do_particle create 14 schloss {-0.1 -0.05 0} 3 2
   do_particle create 14 T23 {-0.1 -0.05 0} 3 2
sq_camera fix oeffnenCam2 1.05 -0.1 -0.75
do_wait time 1.0
+set_anim [Getobjref Riesentor 0] riesentor.oeffnen 0 1
+call_method [Getobjref Riesentor 0] set_open
do_wait time 0.4
   sq_pen move T1 {-1.5 -0.3 0.1}
   sq_pen move T3 {-1.5 -0.3 0.1}
   sq_pen move T4 {-1.5 -0.3 0.1}
   sq_pen move schloss {-1.5 -0.3 0.1}
   sq_pen move T5 {-1.5 -0.3 0.1}
   sq_pen move T2 {-1.5 -0.3 0.1}
   sq_pen move T23 {-1.5 -0.3 0.1}
   do_particle create 14 T1 {-1 -0.05 0} 23 2
   do_particle create 14 T3 {-1 -0.05 0} 23 2
   do_particle create 14 T4 {-1 -0.05 0} 23 2
   do_particle create 14 T23 {-1 -0.05 0} 23 2
   do_particle create 14 T5 {-1 -0.05 0} 23 2
   do_particle create 14 schloss {-1 -0.05 0} 23 2
   do_particle create 14 T2 {-1 -0.05 0} 23 2
sq_camera fix zaubern 1.15 -0.3 0.8
do_action anim protecteyes 0
   sq_pen move T1 {0 0 -1}
   sq_pen move T3 {0 0 1}
   sq_pen move T4 {0 0 -0.5}
   sq_pen move schloss {0 0.5 -1}
   sq_pen move T5 {0 0 1}
   sq_pen move T2 {0 -0.5 0}
   sq_pen move T23 {0 0 0.5}
   do_particle create 14 T1 {-8 -0.05 0} 23 2
   do_particle create 14 T23 {-8 -0.05 0} 23 2
   do_particle create 14 T5 {-8 -0.05 0} 23 2
   do_particle create 14 schloss {-8 -0.05 0} 23 2
   do_particle create 14 T3 {-8 -0.05 0} 23 2
   do_particle create 14 T4 {-8 -0.05 0} 23 2
   do_particle create 14 T2 {-8 -0.05 0} 23 2
do_wait time 0.5
   sq_pen move T1 {-3 0.5 0}
   sq_pen move T3 {-3 0 1}
   sq_pen move T4 {-3 -0.5}
   sq_pen move schloss {-3 -0.5 -1}
   sq_pen move T5 {-3 0.5 0.5}
   sq_pen move T2 {-3 -0.5 1}
   sq_pen move T23 {-3 -0.5 0}
   do_particle create 14 T23 {-8 -0.05 0} 23 2
   do_particle create 14 T3 {-8 -0.05 0} 23 2
   do_particle create 14 T1 {-8 -0.05 0} 23 2
   do_particle create 14 T4 {-8 -0.05 0} 23 2
   do_particle create 14 schloss {-7 -0.05 0} 23 2
   do_particle create 14 T5 {-8 -0.05 0} 23 2
   do_particle create 14 T2 {-8 -0.05 0} 23 2
do_wait time 0.5
   sq_pen move T1 {0 0.5 0}
   sq_pen move T4 {0 -0.5 0}
   sq_pen move schloss {0 0.5 0}
   sq_pen move T5 {0 0 -0.5}
   do_particle create 14 T1 {-8 -0.05 0} 23 2
   do_particle create 14 schloss {-7 -0.05 0} 23 2
   do_particle create 14 T2 {-8 -0.05 0} 23 2
   do_particle create 14 T4 {-8 -0.05 0} 23 2
   do_particle create 14 T3 {-8 -0.05 0} 23 2
   do_particle create 14 T23 {-8 -0.05 0} 23 2
   do_particle create 14 T5 {-8 -0.05 0} 23 2
do_wait time 0.5
   sq_pen move T1 {-3 -1 -0.5}
   sq_pen move T3 {-3 0.5 0.5}
   sq_pen move T4 {-3 -0.5 -0.5}
   sq_pen move schloss {-3 -0.5 -1}
   sq_pen move T5 {-3 0 1}
   sq_pen move T2 {-3 -0.5 0}
   sq_pen move T23 {-3 0 -1}
   do_particle create 14 T5 {-8 -0.05 0} 23 3
   do_particle create 14 schloss {-7 -0.05 0} 23 3
   do_particle create 14 T1 {-8 -0.05 0} 23 3
   do_particle create 14 T23 {-8 -0.05 0} 23 3
   do_particle create 14 T2 {-8 -0.05 0} 23 3
   do_particle create 14 T4 {-8 -0.05 0} 23 3
   do_particle create 14 T3 {-8 -0.05 0} 23 3
sq_actor actionlist 1 {}
do_action anim magicstop 1
#-----Ende der Zauberey-----

sq_wait none
sq_camera fix zwischen_den_beiden 1 0.0 -0.3
do_action anim mann.bowl_gewinnen 0
   sq_pen move T1 {-3 -1 -0.5}
   sq_pen move T3 {-3 0.5 0.5}
   sq_pen move T4 {-3 -0.5 -0.5}
   sq_pen move schloss {-3 -0.5 -1}
   sq_pen move T5 {-3 0 1}
   sq_pen move T2 {-3 -0.5 0}
   sq_pen move T23 {-3 0 -1}
   do_particle create 14 T5 {-8 -0.05 0} 23 3
   do_particle create 14 schloss {-7 -0.05 0} 23 3
   do_particle create 14 T1 {-8 -0.05 0} 23 3
   do_particle create 14 T23 {-8 -0.05 0} 23 3
   do_particle create 14 T2 {-8 -0.05 0} 23 3
   do_particle create 14 T4 {-8 -0.05 0} 23 3
   do_particle create 14 T3 {-8 -0.05 0} 23 3
do_action rotate left 1
do_wait time 0.5
do_text 040b_h 1 {Auto} Sobitteschoen;#So, bitteschön.
do_wait time 0.5
do_action anim cheer 0
do_wait time 1.5
do_text 040b_i 0 {Auto} Undder;#Und der Ring?
do_wait time 2
do_text 040b_j 1 {Auto} Wiering;#Wie Ring?
#do_wait time 3
sq_camera fix 0 0.65 0 0.3
sq_wait none
do_action run SchPlTor 1
do_wait time 0.5
do_action rotate 1 0
sq_actor express 0 bad_normal
do_wait time 0.5
do_action rotate 1 0
do_wait time 0.5
do_action rotate 1 0
do_wait time 0.5
do_action rotate 1 0
sq_wait 1
do_action beam SchPlTor 1
sq_wait 0
do_text 040b_k 0 {Auto} Wiejetzt ;#Wie jetzt?

sq_pen set Elf_Pos_01 SchPlTor
sq_pen set Elf_Pos_02 SchPlTor
sq_pen set TorW SchPlTor
sq_pen setz TorW 10
sq_pen move TorW {-1 0.6 0}
sq_pen setz Elf_Pos_02 13
sq_pen move Elf_Pos_02 {-2 -1 0}
sq_pen setz Elf_Pos_01 16
sq_pen move Elf_Pos_01 {5 2 0}
sq_pen move Elf_Pos_03 {-1.5}
sq_pen set elfenkamera Elf_Pos_02
sq_pen move elfenkamera {0 0.5 0}
sq_pen set hochkommen TorW
sq_pen move hochkommen {1.5 0 -1}
sq_pen set TorW2 TorW
sq_pen move TorW2 {1 0 -1}
do_action beam hochkommen 1
do_action rotate left 1
do_elf path Elf_Pos_01 Elf_Pos_02 0.5

sq_camera fix SchPlTor 1.2 0.0 0.0

sq_wait elf
do_action walk TorW2 1
do_elf lookat 1
do_elf text 040b_l {} Jetztgib;#Jetzt gib

sq_wait none
do_action walk TorW 1
do_wait time 0.4
do_text 040b_m 1 {Auto} Wasaber;#Was? Aber... ich...
do_wait time 2

sq_wait elf
sq_pen move Tor {-1 0 0}
+do_action beam Tor 0
do_elf text 040b_n {} Gibihm;#Gib ihm
do_wait time 0.3
sq_camera fix elfenkamera 1 0 0
do_elf lookat
sq_actor express 1 bad_normal
do_wait time 0.3
do_elf text 040b_o {} Odervon;#...oder von ihm
sq_camera fix SchPlTor 1.2 0.0 0.0
do_elf lookat 1
do_elf text 040b_p {} Also;#Also?!
do_wait time 1

sq_camera move Tor 1.2 -0.3 0.8 0.3
sq_pen set Elf_Pos_03 Tor
sq_pen move Elf_Pos_03 {-1 -1 3}
sq_pen set Elf3Kam Tor
sq_pen move Elf3Kam {-0.5 -0.75 1.5}
do_elf path Elf_Pos_02 Elf_Pos_03
do_wait time 3

sq_camera fix Elf3Kam 1 -0.3 -0.4
do_action rotate Elf_Pos_03 0
do_elf lookat 0
do_elf text 040b_q {} Ihrmuesst;#Ihr müsst
+do_elf hide
sq_camera fix 0 1 0 0.5
do_wait time 0.3
sq_wait 1
do_action beam bePosZ 1
do_action rotate 1 0
sq_pen set zurueckgeb Tor
sq_pen move zurueckgeb {-1.5 0 0}
do_action walk zurueckgeb 1

sq_wait 0
do_text 040b_r 1 {Auto} Hierverliert
do_action rotate zurueckgeb 0
do_action rotate Tor 1
do_wait time 0.2
sq_wait none
do_action anim offerjoint 1
do_action anim offerjoint 0
do_wait time 1
link_obj [Object 0] [Actor 0] 0
do_wait time 0.5
sq_actor express 0 good_normal
do_wait time 1
+do_toolputaway 0
do_wait time 2

do_text 040b_s 0 {PosReac} Naaber

do_action walk bePosZ 1
do_wait time 1
do_action rotate right 0
do_wait time 0.5
do_action anim lookup 0
do_wait time 1
+do_action beam SchPlTor 1
+do_action rotate left 1
+set_anim [Actor 1] mann.schlafen_boden_loop 0 2
+sq_object delete all

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker metall [get_pos this]
+adaptive_sound changethemenow atmometall
#-----------------------------------------

