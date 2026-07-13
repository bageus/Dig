sq_text file Schwefel
sq_audio open swf_050
sq_camera selset inout
sq_wait all

+sq_pen set Becher [Getobjpos Eierbecher]
+sq_pen set Schlafplatz Becher
+sq_pen move Schlafplatz {-2 0 3}
+sq_pen set vormSchlafplatz Schlafplatz
+sq_pen move vormSchlafplatz {1 0 -0.2}
+sq_pen set Mama1 Schlafplatz
+sq_pen move Mama1 {0.2 0 1.4}
+sq_pen set Mama2 Schlafplatz
+sq_pen move Mama2 {-0.8 0 0}
sq_pen set vormBecher Becher
sq_pen move vormBecher {0 0 1.5}
sq_pen set Sicherheitsabstand vormBecher
sq_pen move Sicherheitsabstand {0 0 1}
sq_color 0 Wiggle1

#sq_pen set absprung vormBecher
#sq_pen move absprung {1 0 1}
sq_pen set absprung vormSchlafplatz

sq_pen set imBecher Becher
sq_pen move imBecher {0 -0.5 0}

sq_pen set Partikel1 imBecher
sq_pen move Partikel1 {0 -0.3 0}
sq_pen set Partikel2 imBecher
sq_pen move Partikel2 {0 0 0.2}
sq_pen set Partikel3 imBecher
sq_pen move Partikel3 {0 0 0.2}
sq_pen set Partikel4 imBecher
sq_pen move Partikel4 {-0.2 0 0}
sq_pen set Partikel5 imBecher
sq_pen move Partikel5 {0.2 0 0}

sq_pen set etwaslinks vormBecher
sq_pen move etwaslinks {-1 0 -0.4}
sq_pen set etwasrechts vormBecher
sq_pen move etwasrechts {1 0 -0.4}
sq_pen set wo_Zweiter_erscheint Becher
sq_pen move wo_Zweiter_erscheint {8 0 6}
sq_pen set Kamera_auf_Zweiten wo_Zweiter_erscheint
sq_pen move Kamera_auf_Zweiten {-1.5 0 0}
sq_pen set wo_Zweiter_hingeht wo_Zweiter_erscheint
sq_pen move wo_Zweiter_hingeht {-4 0 -1}
sq_pen set endeBaby wo_Zweiter_hingeht
sq_pen move endeBaby {-1 0 0.1}
sq_pen set aufstehkamera1 Schlafplatz
sq_pen move aufstehkamera1 {-24.5 13.9 -200}
sq_pen set aufstehkamera2 Schlafplatz
sq_pen move aufstehkamera2 {-24.5 13.1 -200}
sq_pen set aufstehkamera3 Schlafplatz
sq_pen move aufstehkamera3 {-23 13.1 -200}

sq_camera move Becher 1.1 -0.3 0.0
do_action walk etwasrechts 0
do_action rotate Becher 0
do_wait time 1

+sq_camera fix vormBecher 0.9 0.0 0.8
do_wait time 0.1
sq_camera move vormBecher 0.9 0.0 -0.8 0.3
do_action anim kontrol 0
do_wait time 0.5
sq_object summon Zwerg wo_Zweiter_erscheint 5
sq_actor find Zwerg 5 1 5 wo_Zweiter_erscheint
call_method [Actor 1] init
#sq_object summon Zipfelmuetze wo_Zweiter_erscheint
#link_obj [Object 1] [Actor 1] 11
sq_color 1 Wiggle2
do_change muetze sparetime 1 auf noanim
do_action walk etwaslinks 0
do_action rotate Becher 0

do_wait time 0.3

sq_wait none
do_action rotate Becher 0
do_action rotate wo_Zweiter_hingeht 1
do_wait time 0.5

sq_camera fix Kamera_auf_Zweiten 0.8 -0.1 0.7
do_text 050a 1 {Auto} Duwillst
#"Willst Du echt das Ei"
do_action walk wo_Zweiter_hingeht 1
sq_object summon Drachen_Ei vormBecher
link_obj [Object 1] [Actor 0] 0
do_wait time 2.2
sq_camera move etwasrechts 1.1 -0.4 -0.1 0.3
do_wait time 1.5
do_action transport vormBecher 0
do_wait time 1.5
sq_camera fix 1 0.7 0.0 -0.4
do_action rotate imBecher 0
do_text 050b 1 {NegAc} Wollenwir
#während die Kamera wegguckt, schmuggeln wir das Ei auf den Becher

#Sascha war hier !
link_obj [Object 1];set_physic [Object 1] 0;set_pos [Object 1] [parse_pos imBecher]


do_action beam Sicherheitsabstand 0;#und stellen die Hauptperson ein Stück weg
do_wait time 0.5
do_action anim standloopa 0
do_wait time 1.5
sq_camera move imBecher 1.0 0.0 -0.9
do_wait time 1
sound play fe_haare 1
do_particle create 0 Partikel1 {0.0 -0.1 0.0} 20 5
do_particle create 0 Partikel2 {0.0 -0.1 0.0} 20 5
do_particle create 0 Partikel3 {0.0 -0.1 0.0} 20 5;    #Huiii....
do_particle create 0 Partikel4 {0.0 -0.1 0.0} 20 5
do_particle create 0 Partikel5 {0.0 -0.1 0.0} 20 5
do_text 050c 1 {NoAnim} Ohist
do_action anim shock 1
do_wait time 0.5
do_particle create 0 Partikel1 {0.0 -0.1 0.0} 20 5
do_particle create 0 Partikel2 {0.0 -0.1 0.0} 20 5
do_particle create 0 Partikel3 {0.0 -0.1 0.0} 20 5;    #Huiii....
do_particle create 0 Partikel4 {0.0 -0.1 0.0} 20 5
do_particle create 0 Partikel5 {0.0 -0.1 0.0} 20 5
do_particle create 0 Partikel1 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel2 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel3 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel4 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel5 {0.0 -0.1 0.0} 1 30
do_action flee wo_Zweiter_erscheint 1
do_wait time 1

sq_camera fix imBecher 1 -0.4 0
do_wait time 2
do_action anim leftright 0
do_wait time 1
do_particle create 0 Partikel1 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel2 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel3 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel4 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel5 {0.0 -0.1 0.0} 1 30
sq_camera move Sicherheitsabstand 0.9 -0.5 0
do_action walk Schlafplatz 0
do_wait time 1.6
do_action rotate left 0
do_wait time 0.3
do_action anim invent_a 0
do_particle create 0 Partikel1 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel2 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel3 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel4 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel5 {0.0 -0.1 0.0} 1 30
do_wait time 2
do_action rotate back 0
do_wait time 0.5
do_action anim scratchhead 0
do_wait time 1
do_action rotate left 0
do_wait time 0.5
do_action anim invent_a 0
do_particle create 0 Partikel1 {0.0 -0.1 0.0} 1 20
do_particle create 0 Partikel2 {0.0 -0.1 0.0} 1 20
do_particle create 0 Partikel3 {0.0 -0.1 0.0} 1 20
do_particle create 0 Partikel4 {0.0 -0.1 0.0} 1 20
do_particle create 0 Partikel5 {0.0 -0.1 0.0} 1 20
do_wait time 2
do_particle create 0 Partikel1 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel2 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel3 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel4 {0.0 -0.1 0.0} 1 30
do_particle create 0 Partikel5 {0.0 -0.1 0.0} 1 30
do_action rotate left 0
do_wait time 0.5
do_action anim laydown 0

start_fade 1 0

do_change muetze sparetime 1 ab noanim;#weg mit der Mütze
do_wait time 1
+sq_object delete 1;#weg mit dem Ei
+do_change muetze transport 1 ab noanim
do_wait time 0.5
+sq_object delete 0;#weg mit dem "Zweiten"

+sq_object summon Drachenbaby absprung;       #"Wie soll er denn heißen?" :-)
do_wait time 0.1
+call_method [Object 0] Editor_Set_Info {{name Drachenbaby} {owner 0}}
+call_method [Object 0] init
+sq_actor find Drachenbaby 10 10 any absprung
sq_camera fix aufstehkamera1 0.65 -0.14 -0.25
sq_color 2 Drache
do_action rotate Schlafplatz 2
do_wait time 1
do_action anim drache01.stehen_anspringen 2
+do_action beam Schlafplatz 0
sq_actor eyes 0 {9}

start_fade 1 1

do_action anim sleepside 0
do_wait time 0.3
do_action anim sleepside 0
do_wait time 0.3
sq_actor eyes 0 {8}
do_action anim sleepside 0
do_wait time 0.3
sq_camera move aufstehkamera2 0.65 -0.14 -0.25 0.4
do_action anim sleeptostand 0
do_wait time 0.4
do_action anim stretch 0
sq_actor eyes 0 {0}
do_wait time 1
sq_camera move aufstehkamera3 0.65 -0.14 -0.25 0.4
#sq_camera move aufstehkamera3 0.65 0 0 0.6
do_action rotate vormSchlafplatz 0
#do_action walk vormSchlafplatz 2
do_wait time 1
#Schrecksekunde

sq_camera move aufstehkamera2 0.8 -0.14 -0.25 0.4
do_wait time 0.3
do_action anim standfronthith 0
#do_text 050c 2
do_wait time 1.7
do_text 050e 2 {NoAnim} Mamamama
do_action anim drache01.stehen_freuen 2
do_wait time 0.5
do_action anim washface 0
do_wait time 1.5
do_action walk Mama1 2
do_wait time 0.3
do_text 050d 0 {NegReac} Gehweg
do_wait time 1.6
do_action rotate 2 0
do_wait time 0.4
do_action rotate 0 2
do_wait time 0.5
do_action anim mann.dialog_ac_negativ_b 0
do_wait time 0.8
do_action anim drache01.sitzen_knuddeln_a 2
do_wait time 0.3
#do_action anim mann.dialog_ac_negativ_b 0
do_wait time 1
do_action anim mann.schwert_ausw_zurueck 0
do_wait time 0.6
do_action walk Mama2 2
do_wait time 0.4
do_action anim mann.dialog_ac_negativ_a 0
do_wait time 1.6
do_action rotate 0 2
sq_camera fix Schlafplatz 0.65 -0.1 -0.1
do_wait time 0.3
sq_actor eyes 0 {l l l l  c c  r r r r}
do_wait time 1
sq_camera move Schlafplatz 0.8 -0.1 -0.1
do_action run wo_Zweiter_hingeht 0
do_action walk wo_Zweiter_hingeht 2
do_wait time 1
do_text 050e 2 {NoAnim} Mamamama2
do_action walk wo_Zweiter_hingeht 2
do_wait time 6.5
do_text " " 2 {NoAnim} Mama Auto Off
do_wait time 1
+do_action beam wo_Zweiter_hingeht 0
+do_action beam endeBaby 2
do_wait time 1


