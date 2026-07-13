#Clip 160 (nicht im Drehbuch) - Zwangsumsiedlung am Ende Kristallwelt

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmokristall
#-----------------------------------------

sq_wait all
sq_camera selset inout
start_fade 1.0 0; #Licht aus

 sq_pen set Tor [Getobjpos Kristalltor]
 sq_pen set Tormusic Tor
 sq_pen set erstermove Tor
 sq_pen move erstermove {25 -5 0}
 sq_pen set zerfallen Tor
 sq_pen move zerfallen {9 -4 0}
 sq_pen set imTor Tor
 sq_pen move imTor {-1.5 0 -5}                     ;#<--- !!!
 sq_pen set startpunkt imTor
 sq_pen move startpunkt {6 0 0}
 sq_pen set brocken2 startpunkt
 sq_pen move brocken2 {2 -4 0}
 sq_pen set p4 imTor
 sq_pen move p4 {4 0 -1}
 sq_pen set brocken p4
 sq_pen move brocken {-1 -5 0}
 sq_pen set EndeKiste imTor
 sq_pen move EndeKiste {-1.5 0 0}                     ;#<--- !!!

 do_wait time 0.5

# +sq_object summon Zwerg startpunkt; #wird eigentlich hergebeamt
# +sq_actor find Zwerg 5 1 any startpunkt
 do_action beam startpunkt 0
 do_change muetze transport 0 auf noanim

 +sq_object summon Feuerstelle Tor
 +call_method [Object 0] packtobox
 +sq_actor find Feuerstelle 5 1 any Tor;#das brauchen wir fürs "Abbruchbeamen" am Schluß

 +sq_actor find Lorelei 15 1 any [Getobjpos Lorelei 0]
 +sq_actor find Kristalltor 15 1 any [Getobjpos Kristalltor 0]

 set_anim [Actor 2] kris_lorelei_kris.durchbruch 0 0
#set_visibility [Actor 2] 1
 call_method [Actor 3] set_open
 set_anim [Actor 3] kris_tor.durchbruch 0 0

 sq_camera fix erstermove 2.5 0 0
 sq_actor express 0 bad_normal
 do_wait time 1
 sound play equake7 1
 -sq_screenvibe equake7;#das Gewackel halt

start_fade 1.0 1; #wieder an und action

link_obj [Actor 1] [Actor 0] 0; #jetzt hat er eine Kiste in der Hand
sq_wait none
do_wait time 1
sq_camera move zerfallen 2.5 0 0 0.2
do_wait time 0.4
sound play lorelei_brocken 0.9
do_wait time 0.6
sound play lorelei_knack 0.7
do_wait time 2
do_action transport p4 0;
do_wait time 0.5
sq_object summon Stein brocken2
do_wait time 1
sq_camera fix 0 0.9 -0.4 0.5
do_wait time 0.2
sound play lorelei_brocken 0.7
sq_camera move p4 0.9 -0.2 0.5 0.08
do_wait time 0.8
sq_object summon Stein brocken
do_wait time 1.2
sound play lorelei_brocken 1
sq_camera fix p4 0.9 -0.2 -0.5
do_action anim shock 0
do_wait time 1

sq_pen set vorZusammenbruch imTor
sq_pen move vorZusammenbruch {1 0 0}
sq_camera move vorZusammenbruch 0.9 -0.4 -0.5 0.08
do_action transport vorZusammenbruch 0
sound play lorelei_crash 1
do_wait time 4

#sound play equake7 1
-sq_screenvibe equake7;#mehr Gewackel
+sq_camera fix imTor 1.3 -0.2 0.7

set_visibility [Actor 3] 0
+set_anim [Actor 2] kris_lorelei.zerfaellt 0 1
#25 Frames

sq_wait none
gametime factor 0.7
sq_camera fix zerfallen 2.5 0 0
do_wait time 1.1
do_action transport imTor 0
sq_camera fix vorZusammenbruch 1 -0.8 0.2
do_wait time 0.7
sq_camera fix Tor 1.5 -0.15 0.7
+gametime factor 1
do_action anim stummble 0
do_wait time 0.6
sound play equake7 1
do_wait time 0.1
+link_obj [Actor 1]
do_wait time 0.2
+do_action beam EndeKiste 1
do_wait time 2;#8
+do_action beam imTor 0
+do_action rotate right 0
+do_change muetze transport 0 ab noanim
do_wait time 0.5
+set_visibility [Actor 3] 1
call_method [Actor 3] set_closed
+set_anim [Actor 3] kris_tor.versperrt 0 0
+set_anim [Actor 2] kris_lorelei_kris.versperrt 0 0

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow atmolava
+adaptive_sound marker lavawelt [parse_pos Tormusic] 1000
#-----------------------------------------
