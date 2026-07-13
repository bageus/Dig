#----------- Clip 095 - erstes treffen auf Knockers ------------------
sq_text file Schwefel ;# Textfile
sq_audio open swf_095
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow knockers
#-----------------------------------------
#sq_actor find Zwerg 150 2 2

#call_method [Actor 1] Editor_Set_Info {{gender male}} ;#und der Sprachausgabe angepaßt
#do_change muetze fight 1 auf noanim

sq_camera selset inout

#----------- SQ_Pen SET
sq_pen set music1 TriggerPos
sq_pen move music1 {12 0 0}
sq_pen set music2 TriggerPos
sq_pen move music2 {39 0 0}
sq_pen set music3 TriggerPos
sq_pen move music3 {125 0 0}
sq_pen set music4 TriggerPos
sq_pen move music4 {129 28 0}
sq_pen set music5 TriggerPos
sq_pen move music5 {144 50 0}
sq_pen set music6 TriggerPos
sq_pen move music6 {109 72 0}
sq_pen set music7 TriggerPos
sq_pen move music7 {45 20 0}

sq_pen set Wachhaus TriggerPos
sq_pen move Wachhaus {0.15 0 0}
sq_pen setz Wachhaus 11.0
sq_pen set Tpos Wachhaus
sq_pen move Tpos {-1.2 0 0.2}
sq_pen set ZWStart 0
sq_pen set ZWMove ZWStart
sq_pen move ZWMove {8.75 0 0}
sq_pen setz ZWMove 13
sq_pen set wech Wachhaus
sq_pen move wech {8 0 0}
sq_pen set wech2 wech
sq_pen move wech2 {2 0 0}
sq_pen set Fahne Wachhaus
sq_pen move Fahne {1.5 0 -0.5}
sq_color 0 Wiggle1
sq_color 1 Knocker1


#----------- Actor 1 (weibl.)
#do_action beam wech 1

#----------- Actor 2 (männl.) - Knockerswache
#do_action beam wech2 2 ;#Wachhaus 2
#do_action rotate front 2

#---gesummonte, neue Wache
sq_pen set summpos TriggerPos
sq_pen move summpos {0.25 0 -0.3}
sq_object summon Zwerg summpos 2
call_method [Object 0] Editor_Set_Info {{name Knockerswache} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg 10 1 2
do_change muetze fight 1 auf noanim
#set_textureanimation [Actor 3] 0 7
#set_textureanimation [Actor 3] 1 7
#set_owner [Actor 3] 2
sq_actor eyes 1 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}

#----------- Actor 0 (Wiggle)
do_action walk ZWMove 0
sq_camera move ZWMove 1.2 0.0 0.0 0.3
do_wait time 3.5
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}

sq_camera fix Wachhaus 1.0 -0.2 0.0

#----------- Knockerswache (Actor 1)
sq_wait 1
do_action anim guardwalk 1
do_action anim guardwalk 1
do_action rotate left 1
do_action anim scout 1
do_wait time 0.75
do_text 095a 1 {Auto} Halthalt ;# HALT! HALT!
do_text 095b 1 {Auto} Werda ;# Wer da?!?!
sq_wait none

do_action walk Tpos 0
#sq_camera move Tpos 0.9 -0.4 -0.3 0.3
#do_wait time 4.5
do_action rotate 0 1
do_wait time 2.0

sq_actor express 1 bad_normal

do_text 095c 1 {Auto} Achnee ;# Ach nee, Wiggles.
do_wait time 1.0
do_action rotate 1 0
do_wait time 1.0

#sq_pen move Tpos {2.5 0 0}

do_text 095d 1 {NegAc} Verziehteuch;# Verzieht euch!
do_wait time 1.0
do_action rotate left 1
do_wait time 0.5
do_action anim scratchhead 0
do_wait time 1.0
do_action anim guardwalk 1
do_wait time 3.0
do_action anim guardwalk 1
   do_action anim leftright 0
   #do_text 095e 0 {NegAc};# Hey!
do_wait time 2.0
sq_pen setz Wachhaus 10
do_action walk Wachhaus 1
do_action walk Tpos 0
do_wait time 1
do_action rotate front 1

sq_camera fix Wachhaus 1.0 -0.2 0.0
do_wait time 0.2
do_text 095f 0 {NegAc} Suchstdu;# Suchst du streit?!
do_wait time 0.5

do_action rotate 0 1
do_action rotate 1 0
do_wait time 1

#----------- Pen verschoben für Camera
sq_pen move Wachhaus {-0.7 0 0}
sq_camera fix Wachhaus 0.65 0 -0.6;#0.8 -0.4 0.4
sq_pen move Wachhaus {0.7 0 0}

sq_wait 1
do_text 095g 1 {NegReac} Was;# HA!
do_text 095h 1 {NegReac} Wigglesund;# Wiggle und kämpfen? ...
do_text 095i 1 {NegAc} Wieodin;# Wie Odin gerade EUCH für die Mission aussuchen konnte.
sq_wait none

#----------- Camera
sq_camera fix Tpos 0.8 0.0 0.0

sq_wait 0
do_action rotate front 0
do_text 095j 0 {PosAc} Weilwir;# Weil wir die Besten der Besten der Besten sind?
do_action anim swordupstart 0
do_action anim sworduploop 0
do_action anim swordupend 0
do_action rotate 1 0
sq_wait none

#----------- Pen verschoben für Camera
sq_pen move Wachhaus {-1.0 0 0}
sq_camera fix Wachhaus 0.8 -0.4 0.4
sq_pen move Wachhaus {1.0 0 0}

sq_wait 1
do_text 095k 1 {NegReac} Diebesten;# Die Besten!?...
sq_camera fix Fahne 0.85 0 -0.8
do_text 095l 1 {NegReac} Wennes;# Wenn es einen würdigen Clan gibt, dann sind WIR das!
do_text 095m 1 {Auto} Diehelden;# Wir sind die wahren Bergbauer!
sq_pen move Wachhaus {-1.0 0 0}
sq_camera fix Wachhaus 0.8 -0.4 0.4
sq_pen move Wachhaus {1.0 0 0}
do_text 095n 1 {Auto} Wirhalten;# Wir halten zusammen, selbst wenn der Drache noch mehr von uns fressen sollte - wir ---
sq_wait none;

sq_wait 0
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}
sq_camera fix 1 0.8 -0.4 -0.3;
do_text 095o 0 {{scratchhead}} Drachewas;# Drache? ... Was für ein Drache?
sq_wait none

sq_wait 1
sq_camera move 1 0.65 -0.4 -0.3 0.05;
sq_actor eyes 1 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}
do_text 095p 1 {{talkacntb} {stretch}} Einriesiges;# Ein RIESIGES KOLOSSALES VIEH.
do_text 095q 1 {Auto} Lautna;# Laut na Legende lauert er in einer Höhle voller Knochen unserer Kumpel.
do_text 095r 1 {Auto} Erspeit;# Er Drache speit mächtiges Feuer und frißt alle, die versuchen, ins Schiff zu gelangen.
sq_wait none

#----------- Pen verschoben für Camera
#sq_pen move Wachhaus {-1.0 0 0}
sq_camera fix Wachhaus 0.8 -0.4 0.4; #0.8 -0.4 -0.6
#sq_pen move Wachhaus {1.0 0 0}

sq_wait 0
do_text 095s 0 {NegReac} Einschiff;# Schiff? .. Unter der Erde?
sq_wait none

#----------- Pen verschoben für Camera
sq_pen move Wachhaus {-1.0 0 0}
sq_camera fix Wachhaus 0.7 -0.4 -0.6;#0.8 -0.4 0.4
sq_pen move Wachhaus {1.0 0 0}

sq_wait 1
do_text 095u 1 {NegAc} Ihrglaubt;# Glaubt ihr nicht!?
do_text 095v 1 {NegAc} Ha;# Ha!
do_text 095w 1 {NegAc} Sehteuch;# Sieh dich vor!
do_text 095x 1 {NegAc} Wennihr;# Wenn der Drache geweckt wird, ist der Untergang nahe.
do_wait time 1
do_text 095y 1 {NegAc} Soihr;# So... und jetzt verzieh dich.
do_action rotate right 0
sq_pen setz Wachhaus 8.5
sq_wait none
sq_wait 1
do_action walk Wachhaus 1
sq_camera move Wachhaus 1.5 0.0 0.0 0.3
do_action rotate front 1
do_wait time 1
+sq_camera get

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker knockers [parse_pos music1] 20
+adaptive_sound marker knockers [parse_pos music2] 30
+adaptive_sound marker knockers [parse_pos music3] 51
+adaptive_sound marker knockers [parse_pos music4] 51
+adaptive_sound marker knockers [parse_pos music5] 51
+adaptive_sound marker knockers [parse_pos music6] 20
+adaptive_sound marker knockers [parse_pos music7] 70
#-----------------------------------------

