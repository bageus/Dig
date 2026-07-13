#Clip 140 - Männer horchen auf Lorelei

sq_text file Kristall
sq_audio open kri_140
sq_wait all
+sq_camera get

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s140
#-----------------------------------------

sq_pen set RaumMitte TriggerPos
sq_pen move ZielAusloeseZwerg {-8 0 0}

sq_actor find PseudoLorelei 200 1;# any loreleioben

#ma schaun, ob das so stimmt...
sq_pen move RaumMitte {57 -24 -3}

#Positionen für die Zwerglein
sq_pen set Frau1Pos RaumMitte
sq_pen move Frau1Pos {-0.9 0 3}
sq_pen set Frau2Pos RaumMitte
sq_pen move Frau2Pos {1 0 2.9}
sq_pen set Mann1Pos RaumMitte
sq_pen move Mann1Pos {5 0 1};
sq_pen set Mann2Pos RaumMitte
sq_pen move Mann2Pos {4 0 0};
sq_pen set Mann3Pos RaumMitte
sq_pen move Mann3Pos {6 0 -1};
sq_pen set linksausdemBild RaumMitte
sq_pen move linksausdemBild {-5 0 0};

sq_camera fix 0 0.8 -0.1 0.7
do_action rotate left 0
do_wait time 1.5
do_action anim leftright 0
do_action anim mann.lauschen_links 0
sq_actor express 0 good_dizzy
               #do_wait time 0.5

               #sq_camera move RaumMitte 0.8 0 0 0.3
               #  do_wait time 1.5
start_fade 1 0
  do_action anim mann.lauschen_links 0
  do_wait time 0.5
  do_text 0140e 0 {NoAnim} dummy {1,1} Off
+call_method [Getobjref PseudoLorelei] start_abducting

  #Zwerge als Objekte erschaffen und Geschlechter zuordnen
  sq_object summon Zwerg Frau1Pos 5
  call_method [Object 0] Editor_Set_Info {{name Frau1} {gender female}}
  call_method [Object 0] init
  sq_object summon Zwerg Frau2Pos 5
  call_method [Object 1] Editor_Set_Info {{name Frau2} {gender female}}
  call_method [Object 1] init
  sq_color 2 Wiggle2
  sq_color 3 Wiggle1
  sq_object summon Zwerg Mann1Pos 5
  call_method [Object 2] Editor_Set_Info {{name Mann1} {gender male}}
  call_method [Object 2] init
  sq_object summon Zwerg Mann2Pos 5
  call_method [Object 3] Editor_Set_Info {{name Mann2} {gender male}}
  call_method [Object 3] init
  sq_object summon Zwerg Mann3Pos 5
  call_method [Object 4] Editor_Set_Info {{name Mann3} {gender male}}
  call_method [Object 4] init

  #Objekte zu Actors, immer in 1 Meter Umkreis um Posi 1 Zwerg mit Besitzer 0 finden
  sq_actor find Zwerg 1 1 5 Frau1Pos
  sq_actor find Zwerg 1 1 5 Frau2Pos
  sq_actor find Zwerg 1 1 5 Mann1Pos
  sq_actor find Zwerg 1 1 5 Mann2Pos
  sq_actor find Zwerg 1 1 5 Mann3Pos
  do_change muetze sparetime 2
  do_change muetze sparetime 3
  do_change muetze metal 4
  do_change muetze wood 5
  do_change muetze transport 6
  sq_actor express 4 good_dizzy
  sq_actor express 5 good_dizzy
  sq_actor express 6 good_dizzy
  sq_camera fix RaumMitte 0.8 0 0
  do_wait time 1;                 #etwas Zeit für die Inits und Mützen
start_fade 1 1

do_action rotate 3 2
do_action rotate 2 3
do_wait time 0.7

do_text 0140a 2 {Auto} Undda
#"Da hat sie ihm das gesagt..."
do_wait time 3.5
do_action rotate 3 TriggerPos
do_wait time 0.5
do_text 0140b 3 {Auto} Pssthoerst
#"Psst! Hörst Du das?"
do_wait time 2
do_action rotate TriggerPos 2
sq_wait none

do_action zombie linksausdemBild 4
do_action zombie linksausdemBild 5
do_action zombie linksausdemBild 6
#here we go

do_wait time 1.5
do_action anim mann.lauschen_links 3
do_wait time 1.5
do_action rotate 4 2
do_wait time 0.5
do_text 0140c 2 {Auto} Wasist
#"Hä? Was ist denn mit denen los?"
do_wait time 2
do_action rotate 4 3
do_wait time 0.5
do_action anim scratchhead 2
do_wait time 1
do_text 0140d 3 {Auto} Maennerein
#"Männer! Ein bißchen Gesäusel..."
do_wait time 1
do_action rotate 4 2
do_wait time 3

sq_wait all
+sq_object delete all
#und weg sind die fünf...

#sq_camera fix loreleioben 0.8 0.1 0.3
#do_wait time 0.2
#sq_camera move loreleiunten 1.8 0.1 0.3
#do_wait time 3

#hier muß das Kristallisieren der gebeamten Männer noch hin...


