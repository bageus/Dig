sq_text file Schwefel
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s072
#-----------------------------------------
+sq_actor find Drache
+sq_color 1 Drache

+set_collision [Actor 1] 1

do_wait camera
sq_wait all

#################Pens#########################

+sq_pen set DrachenCamPos 1
+sq_pen move DrachenCamPos {0 -2 0}

+sq_pen set FleePos 1
+sq_pen form FleePos RowVerUp 5
+sq_pen move FleePos {5 0 -2}

+sq_pen set TrollCamPos 1
+sq_pen move TrollCamPos {-10 0 2}

######TrollSummon#########
+sq_pen set Troll1SummonPos 1
+sq_pen move Troll1SummonPos {-16 0 4}

+sq_pen set Troll2SummonPos 1
+sq_pen move Troll2SummonPos {-15 0 3.9}

+sq_pen set Troll3SummonPos 1
+sq_pen move Troll3SummonPos {-14 0 3.8}
##########################


+sq_pen set Troll2Pos TrollCamPos
+sq_pen move Troll2Pos {3.5 0 2}

+sq_pen set Troll1Pos TrollCamPos
+sq_pen move Troll1Pos {3. 0 0}

+sq_pen set Troll3Pos TrollCamPos
+sq_pen move Troll3Pos {4.5 0 -2}

#############################################

+sq_pen set FightPos TrollCamPos
+sq_pen move FightPos {1 0 -1}
+global TrollSummonPos

+sq_pen move DrachenCamPos {-4 -0.5 0}
sq_camera move DrachenCamPos 1.3 0.3 0.7
do_wait time 4
+sq_pen move FightPos {2 0 0}
sq_camera move FightPos 1.0 -0.2 -0.7 0.5

sq_object summon Troll Troll1SummonPos
sq_object summon Troll Troll2SummonPos
sq_object summon Troll Troll3SummonPos

sq_actor find Troll 26 3
sq_actor find Zwerg 15 5 0 Troll1Pos
#5-9
sq_object summon Streitkolben
sq_object summon Hellebarde
sq_object summon Streitaxt

link_obj [Object 3] [Object 0] 0
link_obj [Object 4] [Object 1] 0
link_obj [Object 5] [Object 2] 0

do_wait time 1

sq_wait none

do_action run FleePos {5 ...}


do_action run Troll1Pos 2
do_action run Troll2Pos 3
do_action run Troll3Pos 4
sq_pen move FightPos {0 0 -2}
do_wait time 2
sq_pen move FightPos {-2 0 -1}

sq_camera move FightPos 0.8 -0.2 -1.0

do_wait time 2
do_wait time 0.2
do_action anim troll.stehen_drohen 2
do_wait time 0.2
do_action anim troll.spies_anschreien_b 3
do_wait time 0.2
do_action anim troll.stehen_fuchteln 4

do_wait time 1
do_action anim troll.stehen_drohen 3
do_wait time 1
do_action anim troll.stehen_fuchteln 2
do_action anim troll.stehen_anschreien 4
do_wait time 1
do_action anim troll.stehen_drohen 3
#Rückgängig
sq_pen move DrachenCamPos {4 0.5 0}

sq_pen move DrachenCamPos {-4 0 -2}
sq_camera fix DrachenCamPos 1.1 -0.4 -0.7
do_action anim troll.stehen_drohen 2
do_action anim troll.stehen_fuchteln 4
sq_wait none
do_action anim drache.sitzen_beissen 1
do_action anim troll.stehen_fuchteln 3
do_action anim troll.spies_anschreien_b 4
do_action anim troll.stehen_fuchteln 2
call_method [Actor 1] set_enemy [Actor 3]
do_action anim troll.stehen_drohen 2
call_method [Actor 1] fire2_start
do_action anim troll.spies_anschreien_b 3
do_action anim troll.stehen_drohen 4
do_wait time 1.0
change_particlesource [Actor 3] 3 27 {0 0 0} {0 0 0} 156 4 0 0 0 1
set_particlesource [Actor 3] 3 1
change_particlesource [Actor 3] 4 6 {0 0 0} {0 0 0} 56 4 0 0 0 1
set_particlesource [Actor 3] 4 1

+call_method [Actor 1] fire_stop
sq_wait 0
sq_wait 1
sq_wait 2
sq_wait 3
sq_wait 4




do_action anim troll.stehen_explosion_tot 3
do_action anim troll.stehen_exp_verwesen 3
do_action anim troll.spies_anschreien_b 4
sq_wait none
do_action anim troll.spies_anschreien_b 2
do_wait time 0.2
do_action anim troll.stehen_fuchteln 4
destruct [Object 3]
destruct [Object 0]
sq_camera fix Troll1Pos 0.8 -0.2 -0.7

do_action anim drache.sitzen_trollspeien 1
do_wait time 0.3
call_method [Actor 1] set_enemy [Actor 2]
call_method [Actor 1] fire2_start
do_action anim troll.spies_anschreien_b 2
do_action anim troll.stehen_fuchteln 4
do_wait time 1.5
set_particlesource [Actor 3] 4 0
set_particlesource [Actor 3] 3 0

change_particlesource [Actor 2] 3 27 {0 0 0} {0 0 0} 156 4 0 0 0 1
set_particlesource [Actor 2] 3 1
change_particlesource [Actor 2] 4 6 {0 0 0} {0 0 0} 156 4 0 0 0 1
set_particlesource [Actor 2] 4 1

+call_method [Actor 1] fire_stop
sq_wait 0
sq_wait 1
sq_wait 2
sq_wait 3
sq_wait 4



do_action anim troll.stehen_explosion_tot 2
do_action anim troll.stehen_exp_verwesen 2



sq_camera fix DrachenCamPos 1.3 0.1 0.7
#destruct [Actor 3]
sq_wait none
do_action anim drache.sitzen_trollspeien 1
do_wait time 0.5

do_action run Troll1SummonPos 4
call_method [Actor 1] set_enemy [Actor 4]
call_method [Actor 1] fire2_start
do_wait time 1.0
set_particlesource [Actor 2] 4 0
set_particlesource [Actor 2] 3 0
do_action anim troll.stehen_hinten_get_tot 4
do_action anim troll.hinten_verwesen 4
change_particlesource [Actor 4] 3 27 {0 0 0} {0 0 0} 156 4 0 0 0 1
set_particlesource [Actor 4] 3 1
change_particlesource [Actor 4] 4 6 {0 0 0} {0 0 0} 156 4 0 0 0 1
set_particlesource [Actor 4] 4 1

destruct [Actor 2]

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changetheme schwefelseen
#-----------------------------------------


+call_method [Actor 1] fire_stop
do_wait time 3
set_particlesource [Actor 4] 4 0
set_particlesource [Actor 4] 3 0
do_wait time 1
destruct [Actor 4]

sq_wait 0
sq_wait 1
sq_wait 2
sq_wait 3
sq_wait 4

sq_camera move Troll1Pos 1.3 -0.2 -0.2
sq_camera get

do_wait time 3
+sq_object delete all

