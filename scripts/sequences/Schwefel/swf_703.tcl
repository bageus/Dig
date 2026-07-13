#CLIP 703 - SCHWEFELBRÜCKE 3
#und noch eine Kettensaege !!!?????
#Wenn Spieler mit Wiggle (#1) mit Eisen kommt / Oder die Schmelze die Einheiten hat:
#Kann man beginnen, die Brücke aufzubauen...
#(Animation, wie ihr möchtet - irgendwie muss halt was passieren mit Brücke und Wiggle und so.)

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------

log "Bruecke 3"
+sq_text file Schwefel
sq_audio open Clip_703

sq_camera fix 0 1.0 -0.2 0

+sq_pen set BasePos	TriggerPos
+sq_pen move BasePos {-0.2 0 1.5 }

#Kameras und Positionen
#+sq_pen set VollbildKamera TriggerPos
#+sq_pen set WiggleRightPos1 TriggerPos
#+sq_pen set WiggleRightPos2 TriggerPos
#+sq_pen set WiggleRightPos3 TriggerPos
#+sq_pen set SummonPos TriggerPos
#+sq_pen set ElfPos TriggerPos
#+sq_pen set ElfPos2 TriggerPos
#+sq_pen set ElfPos3 TriggerPos
#+sq_pen set BrueckeZoomOut TriggerPos
#+sq_pen set BrueckeEndPos TriggerPos

#+sq_pen set AnlehnenPos TriggerPos
#+sq_pen set TiredStartPos TriggerPos
#+sq_pen set TiredEndPos TriggerPos
#+sq_pen set HopsePos TriggerPos
#+sq_pen set HopseStartPos TriggerPos

#+sq_pen set HammerFallPos TriggerPos

+sq_pen set VollbildKamera BasePos
+sq_pen set WiggleRightPos1 BasePos
+sq_pen set WiggleRightPos2 BasePos
+sq_pen set WiggleRightPos3 BasePos
+sq_pen set SummonPos BasePos
+sq_pen set ElfPos BasePos
+sq_pen set ElfPos2 BasePos
+sq_pen set ElfPos3 BasePos
+sq_pen set BrueckeZoomOut BasePos
+sq_pen set BrueckeEndPos BasePos

+sq_pen set AnlehnenPos BasePos
+sq_pen set TiredStartPos BasePos
+sq_pen set TiredEndPos BasePos
+sq_pen set HopsePos BasePos
+sq_pen set HopseStartPos BasePos

+sq_pen set HammerFallPos BasePos

+sq_pen move VollbildKamera {2.5 -0.2 3}
+sq_pen move WiggleRightPos1 {3 0 2.3}
+sq_pen move WiggleRightPos2 {0.6 0 2.3}
+sq_pen move WiggleRightPos3 {-1.2 0 -1.3}
+sq_pen move SummonPos {-7 0 -1.3}
+sq_pen move ElfPos {0.5 -0.4 10}
+sq_pen set ElfBeamPos ElfPos
+sq_pen move ElfBeamPos {15 0 0 }
+sq_pen move ElfPos2 {22.5 -0.3 10}
+sq_pen move ElfPos3 {22.5 -0.3 10}
+sq_pen move BrueckeZoomOut {17 -0.2 3}
+sq_pen move BrueckeEndPos {30 0 3.3}

+sq_pen move AnlehnenPos {25.4 0 2.7}
+sq_pen move TiredStartPos {18 0 3}
+sq_pen move TiredEndPos {23 0 3}
+sq_pen move HopsePos {20 0 3};#3.3
+sq_pen move HopseStartPos {13 0 3};#3.3

#Wiggle lauscht und freut sich
sq_actor actionlist 0 {{anim mann.lauschen_links} {anim applaud}}
do_action rotate right 0
sq_camera move 0 1.0 -0.2 0 0.3

#Bau-Animationenbild aufbauen
sq_object summon Zwerg WiggleRightPos1 6
call_method [Object 0] Editor_Set_Info {{name Derek} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg 10 1 6 WiggleRightPos1
do_wait time 0.1

sq_object summon Zwerg WiggleRightPos2 6
call_method [Object 1] Editor_Set_Info {{name Marko} {gender female}}
call_method [Object 1] init
do_wait time 0.2
sq_actor find Zwerg 10 1 6 WiggleRightPos2
do_wait time 0.1
do_action rotate left 1
do_action rotate right 2
do_wait time 0.5
do_change muetze wood 1 auf noanim
do_change muetze metal 2 auf noanim
do_wait time 0.5

sq_pen set KistenPos WiggleRightPos2
sq_pen move KistenPos {0.7 0 0}

sq_object summon Halbzeug_kiste KistenPos
call_method [Object 2] change_look geschlossen
do_wait time 0.1

sq_pen set BrettPos WiggleRightPos2
sq_pen move BrettPos {0.7 -0.4 0}
sq_object summon Halbzeug_holz BrettPos
call_method [Object 3] change_look brett
do_wait time 0.1

sq_object summon Hammer WiggleRightPos1
link_obj [Object 4] [Object 0] 0
do_wait time 0.1

sq_pen set IronPos1 WiggleRightPos1
sq_pen move IronPos1 {-0.7 0 0}
sq_object summon Eisen IronPos1
do_wait time 0.1

sq_pen set SpaenePos WiggleRightPos2
sq_pen move SpaenePos {0.7 -0.5 0}
sq_pen set IronPos1 WiggleRightPos1
sq_pen move IronPos1 {-0.7 -0.3 0}

#-------------1------------------
do_action anim hammerstart 1
do_action anim foxtailstart 2

#Wiggle 1: und Wiggle 3?? Bau Animationen Actions ... Haemmern, Sägen, Was es sonst noch gibt
#Kamera auf bekannte Position
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }

sq_camera move VollbildKamera 1.2 -0.2 0 0.3

do_wait time 0.1
#-------------2------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_wait time 0.4

#Wiggle 2: Action Mitten im Bauen kommt rein:
#-------------3------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_action walk WiggleRightPos3 0
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_wait time 0.4

#-------------4------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_wait time 0.4

sq_actor eyes 2 {c c 9 c c c c c c 9 c c c c c c 9 c c c c c c 9 c c c c c c 9 c c c  c c 9 c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c 9 c c c 9 c c c c 9 c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c  c c 9 c c c 9 c c c c c c c c 9 c c c c 9 c c c 9 }
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

#-------------5------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_wait time 0.4

#-------------6------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_wait time 0.4

#-------------7------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_wait time 0.4

#Wiggle 2: sieht dem anderen beim Schuften zu.
#-------------8------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
sq_actor eyes 0 {c 9 c c c c c 9 c c c c c 9 c c 9 c}
do_action rotate 1 0
do_wait time 0.4

#-------------9------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_action rotate front 0
do_wait time 0.4

#-------------10------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_action anim put 0
sq_pen set IronPos2 0
sq_pen move IronPos2 {0 0 1}
sq_object summon Eisen IronPos2
do_wait time 0.4

#-------------11------------------
do_action anim tooltakeout_a 0
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
sq_object summon Hammer SummonPos
link_obj [Object 7] [Actor 0] 0
do_elf beam ElfBeamPos
do_wait time 0.4

#Elfe Action kommt an,
#-------------12------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_action anim hammerloopmetall 0
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
sq_pen move IronPos2 {-0.20 -0.26 0}
do_particle create 18 IronPos2 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos2 {-0.05 -0.05 0} 15 2
do_elf move ElfPos
do_wait time 0.4

#-------------13------------------
do_action anim hammerloopmetall 0
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_particle create 18 IronPos2 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos2 {-0.05 -0.05 0} 15 2
do_wait time 0.4

#Elfe Action schaut arbeitende Wiggle 1 an
#-------------14------------------
do_action anim hammerloopmetall 0
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_particle create 18 IronPos2 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos2 {-0.05 -0.05 0} 15 2
do_elf lookat 1
do_wait time 0.4

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
sq_actor eyes 2 {c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c }

#-------------15------------------
do_action anim hammerloopmetall 0
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 10 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 15 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_particle create 18 IronPos2 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos2 {-0.05 -0.05 0} 15 2
do_wait time 0.4


#-------------16------------------
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_particle create 18 IronPos2 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos2 {-0.05 -0.05 0} 15 2
do_elf text 703aa {reden_a} DasSchafft
do_wait time 0.4

#-------------17------------------
do_action anim hammerloopmetall 0
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_particle create 18 IronPos2 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos2 {-0.05 -0.05 0} 15 2
do_wait time 0.4

#-------------18------------------
do_action anim hammerloopmetall 0
do_action anim hammerloopmetall 1
do_action anim foxtailloop 2
do_particle create 17 SpaenePos {0.01 -0.1 0} 5 1
do_particle create 26 SpaenePos {-0.01 -0.1 0} 10 1
do_particle create 17 SpaenePos {0 0 0} 5 1
do_particle create 26 SpaenePos {0 0 0} 10 1
do_particle create 18 IronPos1 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos1 {-0.01 -0.03 0} 15 2
do_particle create 18 IronPos2 {0.05 -0.05 0} 15 2
do_particle create 18 IronPos2 {-0.05 -0.05 0} 15 2
sq_camera move BrueckeZoomOut 2.5 -0.2 0 0.3
#do_wait time 0.35

#Kamera ABBLENDE
+start_fade 2 0
do_wait time 2.5

#Intern neues Bild oder Höhlenbild einstellen
+rem_material;# dann wird das hergebrachte Eisen und Holz gelöscht
+set_anim [Getobjref Schwefelbruecke] swf_bruecke.ganz 0 0
+call_method [Getobjref Schwefelbruecke] set_repaired
# anims der bruecke: bau_a,bau_b,bau_c,bau_d,bau_e,bau_f
do_elf lookat

sq_color 2 Offtext
do_text 703e 2 Auto Auto Auto Force
sq_color 2 2

do_wait time 0.1
link_obj [Object 4]
do_action beam AnlehnenPos 2
do_action beam TiredStartPos 0
do_action beam HopseStartPos 1
do_wait time 0.2

link_obj [Object 7] [Actor 0] 0
do_action rotate left 2
do_action rotate right 0
+sq_pen set HopseStartPos TiredEndPos
+sq_pen move HopseStartPos {-3 0 3};#3
do_action beam HopseStartPos 1

do_wait time 1
#do_text 006ad 0 NegReac Verschon Auto Off

#Kamera AUFBLENDE
+start_fade 6 1
do_action rotate front 1
do_wait time 2

sq_actor actionlist 2 {{anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop} {anim leanloop}}
sq_actor eyes 2 {c c 9 c c c c c c 9 c c c c c c 9 c c c c c c 9 c c c c c c 9 c c c  c c 9 c c c c 9 c c c c c 9 c c c c c c 9 c c c c c c c c c c c c 9 c c c 9 c c c c 9 c c c c c 9 c c c c c c c c c c c c 9 c c c c 9 c c c  c c 9 c c c 9 c c c c c c c c 9 c c c c 9 c c c 9 }
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }

do_action anim leanstart 2
do_action walktired TiredEndPos 0
sq_actor actionlist 1 {{anim jumpa} {anim jumpa} {anim jumpa} {anim scratchhead} {anim bend} {anim scratchhead} {anim jumpa} {anim cough}}
do_action rotate front 1

+sq_camera move TiredEndPos 1.3 -0.2 0 0.2
do_wait time 7

#Situation und Bild Brücke ist fertig - Elfe und Wiggle #2 stehen da, wie zuvor.
#Wiggle 1 Action: ist völlig fertig. (Sitzt irgendwo, lehnt - fächelt sich Luft zu etc.)

do_action rotate front 0
#Wiggle 2 Action: geht zur fertigen Brücke, beginnt drauf rumzuspringen, dann hält er inne:
#do_action skipp HopsePos 1
+sq_pen set HammerFallPos 0
+sq_pen move HammerFallPos {-0.5 0 0}

link_obj [Object 7]
sq_object beam [Object 7] HammerFallPos
do_elf move ElfPos2
do_wait time 1

sq_actor express 0 bad_dizzy
sq_actor idleanim 0 Standard
sq_actor eyes 0 {c c 9 c c c c 9 c c c c 9 c c c c 9 c c c 9 c c 9 c c c c 9 c c c c 9 c c c c c c c 9 c c 9 c c c c c c c c 9 c c c c c c c 9 c c 9 c c c c c c c c 9 c c c c c c c 9}
do_wait time 3

sq_actor actionlist 1 {{anim jumpa} {anim jumpa} {anim jumpa} {anim jumpa} {anim scratchhead}}
sq_actor eyes 1 { c c c c c c c c c c c c c c c c c 9 c c 9 c c 9 c c 9 9 c c 9 9 c c 9 9 c c c c c}
do_action rotate right 1
do_wait time 7

#Wiggle 2 Text: (abfällig) Das hält nicht lang'! Höchstens nen Tag!
do_text 703a 1 Auto Dashaelt
sq_actor idleanim 0 Standard
do_elf lookat 0
do_wait time 2
#Elfe Action: geht etwas näher zu Wiggle 1 und/oder 3
do_wait time 1

#Elfe Text: Jau! Da hat dein winziger Freund recht. Das hält nicht lang! Manchmal seid ihr doch schlauer als ich denke...

sq_actor actionlist 1 {{anim standloopb} {anim standloopc} {anim scratchhead} {anim standloopa} {anim standloopc} {anim standloopa}}
do_action anim standloopa 1
do_elf text 703b {zustimmen|reden_a} WoEr
do_wait time 5
#Elfe Action: zu Spieler zurückdrehen

do_elf lookat
do_wait time 1
#Elfe Text: Solltest wohl besser schnell umziehen!
do_elf text 703c {auffordern} Du
do_action walk BrueckeEndPos 2
#do_action sneak TriggerPos 1
do_action sneak BasePos 1

do_wait time 3
#do_action run HopseStartPos 1
#do_wait time 1
#Evt. Kamera Zoom auf Wiggle 1
#Wiggle 1 Action: bricht erschöpft zusammen - klatsch.
do_text 703d 0 {{sitdown}} Uahh
do_action anim sitdown 0

do_wait time 3
+do_elf hide
+do_action anim standup 0
do_wait time 0.3
+do_change muetze wood 1 ab noanim
+do_wait time 0.5
+do_change muetze metal 2 ab noanim
do_wait time 0.5

#Szene Ende
+sq_object delete all
do_wait time 1
#+sq_camera get

#703a Das hält nicht lang'! Höchstens nen Tag!
#703b Jau! Da hat dein winziger Freund recht. Das hält nicht lang! Manchmal seid ihr doch schlauer als ich denke..
#703c Solltest wohl besser schnell umziehen!
#703d Uahh!

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow metall
#-----------------------------------------

