+activate_digcheck
sq_audio open 0085
sq_text file Schwefel
#---inserted-by-Jan---MUSIC---------------
adaptive_sound markerdisable
adaptive_sound changethemenow s085
#-----------------------------------------
sq_camera follow 0 1.3
sq_wait camera

+sq_pen set FakePos TriggerPos
+sq_pen move FakePos {-4 -6 0}
sq_object summon Zwerg FakePos

+sq_actor find Zwerg 10 1
+sq_actor find Drache

+sq_pen set WigglePos TriggerPos
+sq_pen set TalkPos TriggerPos
+sq_pen set UpDrachePos TriggerPos
+sq_pen set WDBildPos TriggerPos
+sq_pen set DWalkPos TriggerPos
+sq_pen set DracheStartPos 2

+sq_pen move WigglePos {-4 0 0}
+sq_pen move UpDrachePos {5 -2.5 -4}
+sq_pen move DracheStartPos {2 -1 0}
+sq_pen move WDBildPos {-4.8 0 0}
+sq_pen move DWalkPos {0 -1 0}


sq_color 2 Drache

sq_wait camera
do_wait time 0.5

+do_action walk WigglePos 0
do_wait time 3
sq_wait all
+sq_pen set WiggleTalkPos 0
+sq_pen move WiggleTalkPos {3 0 0}

sq_pen move WiggleTalkPos {3 0 0}
sq_camera move DracheStartPos 1.3 -0.4 0.4
-sq_sound Gaehn 2
do_action anim drache.liegen_d_zu_schulter_r_d_werschleicht 2
#do_action anim drache.schulter_r_warten 2
do_action anim drache.schulter_r_warten 2
#do_text 085a 2 {drache.liegen_d_zu_schulter_r_d_werschleicht drache.schulter_r_warten drache.schulter_r_warten drache.schulter_r_warten} Gaehn
sq_camera move UpDrachePos 1.3 0.2 0.4
sq_wait none
#sq_sound Wer_stoert 2
#do_text 085a 2 {drache.schulter_r_d_willman} Wer_stoert
sq_sound Wer_stoert 2
do_wait time 0.5
do_action anim drache.schulter_r_d_willman 2
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt2 1
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt1 1
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt2 1
do_wait time 2
sq_wait all
+do_action rotate WigglePos 1
sq_actor eyes 0 { o }

+state_triggerfresh [Actor 2] idle

sq_wait all
+do_action rotate TriggerPos 0
+sq_pen set AehPos WDBildPos
+sq_pen move AehPos {2.5 -0.5 1}
sq_camera fix AehPos 0.8 -0.3 -0.9
sq_wait none
sq_wait 0
+sq_pen move TalkPos {4 0 -2.6}
+do_action rotate WigglePos 2
+do_action walk TalkPos 2
do_text 085b 0 Auto Wenn_du


sq_wait none
sq_wait 2
+sq_pen move WDBildPos {3 0 0}
+sq_pen set SchnueffPos WDBildPos
+sq_pen move SchnueffPos {-1 -0.8 0}
sq_camera move SchnueffPos 0.9 -0.1 -0.4
do_action anim drache.sitzen_d_ahmann_start 2
do_action anim talkrepob 0
sq_actor eyes 0 {2}
-sq_sound Schnueff 1
do_action anim drache.sitzen_d_ahmann_loop 2
do_action anim drache.sitzen_d_ahmann_loop 2
+sq_pen move WDBildPos {0 -1.5 0}
sq_camera fix WDBildPos 1.3 0.1 0.9
do_action anim drache.pumpe_zu_sitzen_end 2
sq_actor eyes 0 {0}
sq_wait none
do_wait time 1
sq_wait 2
do_text 085c 2 {drache.sitzen_d_somuesst drache.sitzen_dialog_g drache.sitzen_dialog_d} Riecht_man
do_text 085c2 2 {drache.sitzen_dialog_d drache.sitzen_dialog_g drache.sitzen_dialog_d} Kein_Bart

sq_camera fix AehPos 0.65 -0.3 -0.9
sq_wait all
do_tooltakeout Axt 0
sq_actor eyes 0 {4}
#do_text 085d 0 { standtosword swordturn swordstillani swordstillani}
do_action anim standtosword 0
do_action anim swordstillani 0
do_action anim swordturn 0
do_action anim swordstillani 0
sq_camera fix WDBildPos 1.3 0.1 0.9
do_text 085e 2 {drache.sitzen_d_dochgenug drache.sitzen_d_dochgenug drache.sitzen_dialog_g drache.sitzen_dialog_d} Haltet_ein
sq_camera fix AehPos 0.8 -0.3 -0.9
do_text 085f 0 Auto Dann_will
+do_toolputaway 0
sq_actor eyes 0 {0}
sq_camera fix WDBildPos 1.3 0.1 0.9
do_text 085g 2  {drache.sitzen_dialog_g drache.sitzen_d_habtdank drache.sitzen_dialog_g drache.sitzen_dialog_g drache.sitzen_dialog_d drache.sitzen_dialog_g}  Vieleicht_ist
do_text 085h 2 {drache.sitzen_dialog_d drache.sitzen_dialog_g drache.sitzen_dialog_d drache.sitzen_dialog_g} Von_Stolz
sq_camera fix AehPos 0.8 -0.3 -0.9
do_text 085i 0  Auto Ja_Hi
do_wait time 1
sq_camera fix WDBildPos 1.3 0.1 0.9
do_text 085j 2 {drache.sitzen_d_somuesst drache.sitzen_dialog_d drache.sitzen_dialog_g} Seid_meine
#sq_camera fix AehPos 0.8 -0.3 -0.9
do_action anim talkpapoa 0
do_text 085k 2 {drache.sitzen_d_somuesst drache.sitzen_dialog_g drache.sitzen_dialog_d drache.sitzen_dialog_g drache.sitzen_dialog_d drache.sitzen_dialog_d drache.sitzen_dialog_d} Ihr_muesst
do_action anim talkpapoa 0
sq_wait none

do_action anim drache.sitzen_d_speienoben 2
do_wait time 0.6
-sq_sound Spei 1
call_method [Actor 2] set_enemy [Actor 1]
#call_method [Actor 2] fire_start
call_method [Actor 2] fire2_start
sq_camera fix WDBildPos 1.3 0.1 0.9
do_wait time 2.5

sq_wait 2
+call_method [Actor 2] fire_stop
+sq_object delete all

do_action anim talkrepob 0
do_action anim protecteyesstart 0
do_action anim protecteyesloop 0
do_action anim protecteyesstop 0
sq_wait all
do_wait time 2
do_text 085l 2 {drache.sitzen_d_dochgenug drache.sitzen_dialog_d drache.sitzen_dialog_d drache.sitzen_dialog_g  drache.sitzen_d_abernein} Wie_Euch
do_text 085m 2 {drache.sitzen_dialog_f drache.sitzen_dialog_d drache.sitzen_dialog_d drache.sitzen_dialog_g drache.sitzen_dialog_g} Nicht_abhalten
do_wait time 1
do_wait camera
sq_camera fix AehPos 0.8 -0.3 -0.9
do_text 085n 0 { wipenose scratchhead} Wir_sollen
#do_text 085o 0 { listenastart listenaloop listenaend} WirSollen_n
do_wait time 1
sq_camera fix WDBildPos 1.3 0.1 0.9
do_text 085p 2 {drache.sitzen_d_dieloesung drache.sitzen_dialog_d drache.sitzen_dialog_g drache.sitzen_dialog_d drache.sitzen_dialog_g} Der_Loesung
#do_text 085q 2 {drache.sitzen_dialog_d drache.sitzen_dialog_d} Grabt_p
sq_camera fix AehPos 0.8 -0.3 -0.9

do_wait time 2
do_wait camera
sq_camera follow 0 1.3
do_wait time 2
+set_roty [Actor 2] 1.57
+set_pos [Actor 0] [parse_pos WigglePos]

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound markerenable
+adaptive_sound changethemenow schwefelseen
#-----------------------------------------

