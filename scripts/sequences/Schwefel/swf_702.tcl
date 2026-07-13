#CLIP 702 - SCHWEFELBRÜCKE 2
#Wenn Spieler mit Wiggle #1 15 Pilzstämme an der Brücke hat geht es hier los

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------

log "Bruecke 2"
# Man braucht mindestens 10 Eisen
#Actor 0 ist der der den letzten Pilzstamm abgelegt hat Wiggles 1
#Actor 1 und 2 werden erzeugt und kommen nacheinander rein; Wiggle 2 und 3

#Anmelden des Textfiles und Audiofiles
+sq_text file Schwefel
sq_audio open Clip_702

+sq_pen set BasePos	TriggerPos
+sq_pen move BasePos {-0.2 0 1.5 }

#Kameras und Positionen
#+sq_pen set VollbildKamera TriggerPos
#+sq_pen set WiggleRightPos1 TriggerPos
#+sq_pen set WiggleRightPos2 TriggerPos
#+sq_pen set WiggleRightPos2b TriggerPos
#+sq_pen set WiggleRightPos2nahe TriggerPos
#+sq_pen set WorkPos TriggerPos
#+sq_pen set SummonPos TriggerPos
#+sq_pen set KillPos TriggerPos
#+sq_pen set WoodWorkPos TriggerPos
#+sq_pen set WoodWorkPosUnten TriggerPos
#+sq_pen set WoodWorkPosUntenLinks WoodWorkPosUnten
#+sq_pen set WorkPosSpaene TriggerPos

+sq_pen set VollbildKamera BasePos
+sq_pen set WiggleRightPos1 BasePos
+sq_pen set WiggleRightPos2 BasePos
+sq_pen set WiggleRightPos2b BasePos
+sq_pen set WiggleRightPos2nahe BasePos
+sq_pen set WorkPos BasePos
+sq_pen set SummonPos BasePos
+sq_pen set KillPos BasePos
+sq_pen set WoodWorkPos BasePos
+sq_pen set WoodWorkPosUnten BasePos
+sq_pen set WoodWorkPosUntenLinks WoodWorkPosUnten
+sq_pen set WorkPosSpaene BasePos

+sq_pen move VollbildKamera {2.5 -0.2 3}
+sq_pen move WiggleRightPos1 {4.5 0 2.3}
+sq_pen move WiggleRightPos2 {3.0 0 2.3}
+sq_pen move WiggleRightPos2b {-1 0 1.8}
+sq_pen move WiggleRightPos2nahe {3.7 0 2.3}
+sq_pen move SummonPos {-3.8 0 -4}
+sq_pen move KillPos {-5.5 0 -4}
+sq_pen move WorkPos {1.3 0 -2}
+sq_pen move WoodWorkPos {0.9 -0.3 -2}
+sq_pen move WoodWorkPosUnten {0.9 0 -2}
+sq_pen move WoodWorkPosUntenLinks {-0.05 0 -2}
+sq_pen move WorkPosSpaene {0.9 -0.5 -2}

+sq_object summon Stein WoodWorkPosUnten

sq_wait none
#Kamera auf bekannte Position
sq_camera move VollbildKamera 1.2 -0.2 0

#Wiggle 1 Text: So! Auf geht's! Wollen wir mal...
do_text 702a 0 PosAc SoAuf
sq_wait all
do_action run WiggleRightPos1 0

sq_wait none
sq_camera fix VollbildKamera 1.2 -0.2 0
#Wiggle 2 Action: kommt herein
sq_object summon Zwerg SummonPos 6
call_method [Object 1] Editor_Set_Info {{name Ati} {gender female}}
call_method [Object 1] init
do_wait time 0.2
sq_actor find Zwerg 25 1 6
do_wait time 0.1
do_change muetze wood 1 auf noanim
do_wait time 0.1

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

#Wiggle 2 Text: (singt wie Kind, schief aber süß) Was aufbau'n, was aufbau'n...
sq_wait none
do_text 702b 1 {} Was
do_action rotate left 0
sq_wait all
do_action skipp WiggleRightPos2b 1

sq_wait none
do_action anim stretch 0
do_action skipp WiggleRightPos2nahe 1
do_wait time 1.2
do_action anim tooltakeout_a 0
do_wait time 0.5
sq_object summon Halbzeug_holz SummonPos
call_method [Object 2] change_look pilzstamm
link_obj [Object 2] [Actor 0] 1
do_wait time 1
sq_actor actionlist 0 {{anim foxtailstart} {anim foxtailstop}}
do_action anim standloopa  0
do_wait time 1.4

sq_wait none
#Wiggle 2 Action: Er bleibt bei Wiggle #1 stehen, der ihm einen Pilzstamm in die Hand drückt.
do_action anim hatofgone 0
do_wait time 0.3
do_action anim put 0
do_action anim put 1
do_wait time 0.3
link_obj [Object 2]
link_obj [Object 2] [Object 1] 0

do_wait time 0.5

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

#Wiggle 2 Action: von der Schwere des Stammes überfordert, taumelt mit dem Ding Richtung Abgrund.
do_action strike WorkPos 1
do_wait time 6.5

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

#sq_object summon Halbzeug_kiste WoodWorkPosUnten
#call_method [Object 2] change_look geschlossen
do_wait time 0.5

link_obj [Object 2]
sq_object beam 2 WoodWorkPos
do_action anim put 1
do_wait time 0.5
sq_actor actionlist 1 {{anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailstop}}
do_particle create 17 WorkPosSpaene {0.01 -0.1 0} 4 10
do_particle create 26 WorkPosSpaene {-0.01 -0.1 0} 7 10
do_action anim foxtailstart 1
sq_actor actionlist 0 {{rotate front} {anim kneebend} {anim kneebend} {anim kneebend} {rotate left}}
do_action anim standloopa 0
do_wait time 1.5


#Eventuell Kamera: Schnitt (Zoom) auf Wiggle 1 und Wiggle 3 sodass Wiggle 2 nicht mehr zu sehen ist
#Wiggle 3 Action: taucht auf.
sq_object summon Zwerg SummonPos 6
call_method [Object 3] Editor_Set_Info {{name Berek} {gender male}}
call_method [Object 3] init
do_wait time 0.2
sq_actor find Zwerg 25 1 6
do_wait time 0.8
do_change muetze arbeitslos 2 auf noanim
do_wait time 0.2

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }

#Wiggle 3 Text: Was soll'n das werden?! Hm?
sq_wait none
do_text 702c 2 Auto Soll
sq_wait all
do_action run WiggleRightPos2 2

sq_wait none
sq_actor actionlist 1 {{anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailstop}}
do_action anim foxtailstart 1
do_action anim talkc 2
do_particle create 17 WorkPosSpaene {0.01 -0.1 0} 2 10
do_particle create 26 WorkPosSpaene {-0.01 -0.1 0} 3 10
#Wiggle 1 Text: Wir bauen die Brücke auf.
do_text 702d 0 {{PosReac} {showleft}} Wir
do_wait time 4

#Wiggle 3 Action: zeigt auf Pilzstämme/Stamm.
#Wiggle 3 Text: Damit?... Nie! Brauchen wir Eisen für. Zehn Einheiten... mindestens! Schmelze bauen!
sq_wait none

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }

sq_actor actionlist 2 {{anim mann.unterhalten_m} {anim scratchhead}}
sq_actor actionlist 1 {{anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailloop} {anim foxtailstop} {rotate front} {anim breathe}}
#do_action rotate 1 2
do_action anim foxtailstart 1
do_particle create 17 WorkPosSpaene {0.01 -0.1 0} 2 10
do_particle create 26 WorkPosSpaene {-0.01 -0.1 0} 3 10
do_text 702e 2 NegReac DamitNie
do_wait time 9

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }

sq_wait none
#Wiggle 1 Action: kratzt sich am Kopf.
do_action anim scratchhead 0
do_wait 0.4
#Wiggle 1 Text: Ach so... Hmmm... gute Idee.
do_text 702f 0 Auto Achso
do_wait time 3.4

#Wiggles 2 Action: (springt cool an den beiden vorbei,
sq_wait none
#Wiggles 2 Text: singt dabei wie Kind, schief aber süß) Was aufbauen, was aufbauen...
sq_actor actionlist 2 {{rotate 1} {anim standloopc} {anim standloopc } {rotate right}}
do_action anim standloopc 2
do_text 702i 1 {} Aufbauen
sq_wait all
do_action skipp KillPos 1
#Wiggle 1 und Wiggle 3 sehen sich an - machen gegenseitig ein "Ist der Wischi-Waschi?!"...
#Wiggle 1 Action sieht Wiggle 3 an
sq_wait none
do_action rotate 0 2
#Wiggle 2 Action sieht Wiggle 1 an
do_action rotate 2 0
do_wait time 0.7
#Wiggle 1 Action Wischi-Waschie
do_action anim getcomfort 0
#Wiggle 3 Action Wischie-Waschie
do_action anim getcomfort 2
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }
do_wait time 3

#Wiggle 3 Action winkt ab
do_action anim talkrenga 2
do_wait time 0.4

#Wiggle 3 Action geht aus dem Bild
sq_actor actionlist 0 {{anim standloopa} {anim scratchhead} {anim breathe} {anim stretch} {rotate front}}
do_action anim standloopb 0
do_action run KillPos 2
do_wait time 6

#sq_wait all
+do_change muetze wood 1 ab noanim
+do_change muetze arbeitslos 2 ab noanim
do_wait time 0.1
+sq_object delete all
+sq_object summon Pilzstamm WoodWorkPosUnten
do_wait time 0.7
#+sq_object summon Stein WoodWorkPosUnten
#do_wait time 0.5

#Szene Ende
#+sq_camera get

#702a So! Auf geht's! Wollen wir mal...
#702b Was aufbau'n, was aufbau'n...
#702c Was soll'n das werden?! Hm?
#702d Wir bauen die Brücke auf.
#702e Damit?... Nie! Brauchen wir Eisen für. Zehn Einheiten... mindestens! Schmelze bauen!
#702f Ach so... Hmmm... gute Idee.
#702g ENTSETZLICH SCHREIEN - ALS WÜRDE ER ABSTÜRZEN!
#702h Nur'n Spass.
#702i Was aufbauen, was aufbauen...

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow metall
#-----------------------------------------

