sq_text file Kristall
sq_camera fix 0 1.2 -0.2 0.6
sq_actor find Zwerg 20 5 3

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s139
adaptive_sound volfact 40
#-----------------------------------------

+sq_pen set WiggleStart 0
+sq_pen set Grammophon [Getobjpos Info_Pos_Zwerg]
+sq_pen set Wiggle TriggerPos
+sq_pen set Kamera1 TriggerPos

+sq_pen set Back1 Grammophon
+sq_pen set Back2 Grammophon
+sq_pen set Back3 Grammophon
+sq_pen set Back4 Grammophon
+sq_pen set Back5 Grammophon
+sq_pen set Kamera2 Grammophon

+sq_pen setz WiggleStart 13

+sq_pen move Back1 { 0 0 5 }
+sq_pen move Back2 { -2.0 0 6 }
+sq_pen move Back3 {0.5 0 3 }
+sq_pen move Back4 {-0.7 0 2.6 }
+sq_pen move Back5 {-1.2 0 1 }
+sq_pen move Wiggle { 3 0 0 }
+sq_pen move Kamera1 { -2.5 0 0 }
+sq_pen move Kamera2 { -6 -1 0 }

do_action beam Back1 1
do_action beam Back2 2
do_action beam Back3 3
do_action beam Back4 4
do_action beam Back5 5

sq_color 1 Brains1
sq_color 2 Brains2
sq_actor actionlist 1 { { { anim standloopa } { anim standloopb } { anim standloopc } } loop }
sq_actor actionlist 4 { { { anim invent_b } { anim calculator } { anim standloopa } { anim standloopb } { anim standloopc } { rotate 3 } { rotate Grammophon } { rotate 5 } } loop }
sq_actor actionlist 3 { { { anim invent_b } { anim calculator } { anim standloopa } { anim standloopb } { anim standloopc } { rotate 4 } { rotate Grammophon } { rotate 5 } } loop }
sq_actor actionlist 5 { { { anim invent_b } { anim calculator } { anim standloopa } { anim standloopb } { anim standloopc } { rotate 3 } { rotate Grammophon } { rotate 4 } } loop }

sq_wait none
do_action rotate Grammophon { 1 3 4 5 }

sq_wait 0
sq_camera selset inout
do_action walk WiggleStart 0
do_action rotate front 0

sq_actor eyes 0 { c 9 9 l l l l 9 9 c c r r r r c c 9 9 c c l l c c r r c }
do_wait time 2
do_action anim lookup 0
do_action anim dontknow 0
do_action rotate left 0
do_action anim discod 0
do_action anim discod 0
do_action anim discoa 0
do_action anim discoa 0
sq_wait none
sq_actor actionlist 0 {{anim discoc} loop}
do_action anim discoc 0
set_vel [Actor 0] {-0.5 0 0.0}

do_wait time 2
start_fade 3 0
do_wait time 3
do_action beam Wiggle 0
+sq_pen move Wiggle { -4.5 0 0 }
sq_wait 2

set_textureanimation [Actor 2] 2 5
set_textureanimation [Actor 2] 1 9
set_textureanimation [Actor 2] 0 9

set_textureanimation [Actor 1] 0 9
set_textureanimation [Actor 1] 1 10
set_textureanimation [Actor 1] 2 3
catch {del [ref_get [Actor 1] myglasses]; ref_set [Actor 1] myglasses 0}

set_textureanimation [Actor 3] 0 9
set_textureanimation [Actor 3] 1 10

set_textureanimation [Actor 4] 0 9
set_textureanimation [Actor 4] 1 10

set_textureanimation [Actor 5] 0 9
set_textureanimation [Actor 5] 1 8

+do_change muetze sparetime 1 auf noanim
+do_change muetze sparetime 3 auf noanim
+do_change muetze sparetime 4 auf noanim
+do_change muetze sparetime 5 auf noanim

#sq_object summon Dummy_Brain_Muetze_a
#link_obj [Object 0] [Actor 1] 4
#sq_object summon Dummy_Brain_Muetze_a
#link_obj [Object 1] [Actor 3] 4
#sq_object summon Dummy_Brain_Muetze_a
#link_obj [Object 2] [Actor 4] 4
#sq_object summon Dummy_Brain_Muetze_b
#link_obj [Object 3] [Actor 5] 4

sq_audio open Kris_139
do_action rotate Wiggle 2
sq_wait none
sq_camera fix Kamera1 1.2 -0.4 -0.2
sq_actor actionlist 0 {}
+set_vel [Actor 0] {0 0 0}
do_wait
start_fade 3 1
do_wait time 2
adaptive_sound volfact 60

sq_wait 0
do_action walk Wiggle 0

sq_actor actionlist 0 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
sq_actor actionlist 2 {}
sq_wait 2
do_action rotate 2 0

do_text 0138aa 2 PosAc 139a
do_text 0138ab 2 PosReac 139b

sq_actor actionlist 0 {}
sq_wait 0
sq_actor actionlist 2 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
do_action anim standloopc 2
sq_camera fix 0 0.8 -0.1 0.8
do_action rotate Wiggle 1
do_text 0138ac 0 NegReac 139c


sq_actor actionlist 2 {}
sq_wait 2
sq_actor actionlist 0 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
+sq_pen move Kamera1 { -1 0 0 }
do_action anim standloopc 0
sq_camera fix Kamera1 1.1 -0.1 0.7
do_text 0138ad 2 PosAc 139d
do_text 0138ae 2 PosReac 139e

sq_actor actionlist 0 {}
sq_wait 0
sq_actor actionlist 2 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
do_action anim standloopc 2
sq_camera fix 0 0.9 -0.1 -0.3
do_text 0138af 0 Auto 139f

sq_actor actionlist 2 {}
sq_wait 2
sq_actor actionlist 0 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
do_action anim standloopc 0
do_action anim talkacngb 2
do_text 0138ag 2 NegReac 139g
do_wait time 0.5

sq_wait none
sq_actor actionlist 2 { { rotate left } loopstart { { anim invent_c } { anim invent_b } } { anim invent_a } loop }
do_action anim scratchhead 2
sq_camera fix 1 0.9 -0.1 0.6
do_action anim impatient 1
+sq_pen move Back1 { -1 0 1 }
do_wait time 1
sq_actor actionlist 1 {}

sq_wait 1
do_action rotate 2 1
do_action rotate 1 0
do_text 0138ah 1 { impatient jumpa} 139h
do_action rotate 2 1
do_action anim jumpa 1
do_action anim pressbutton 1
do_action rotate 0 1
do_text 0138ai 1 PosReac 139i

sq_wait 0
do_text 0138aj 0 { insane } 139j

sq_actor actionlist 2 { { rotate 0 } }
sq_wait 1
sq_actor actionlist 0 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
+sq_pen move Kamera1 { 1 0 0 }
sq_camera fix Kamera1 1.2 -0.4 -0.2
do_text 0138ak 1 { scratchhead talkrenta } 139k
sq_wait 2
do_action rotate 2 0
+sq_pen move Kamera1 { 1 0 0 }
do_text 0138al 2 PosAc 139l
sq_camera fix Kamera1 0.9 -0.1 -0.3
do_action rotate 1 0
do_action anim talkacnga 0
sq_wait 1
sq_actor actionlist 0 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
do_action anim talkrepoa 1
do_action rotate 2 1
do_text 0138am 1 { talkacnta } 139m

sq_actor actionlist 2 {}
sq_wait 2
do_action rotate 2 0
do_text 0138an 2 Auto 139n
sq_camera fix 2 0.8 -0.1 0.2
do_text 0138ao 2 PosAc 139o

+sq_pen move Kamera1 { -2.5 0 0 }
+sq_pen move Back2 { -3 0 1 }
+sq_pen move Wiggle { -2.2 0 -1 }

do_action anim talkacpoc 2

sq_camera fix 0 1.2 -0.2 -0.8
sq_wait 1
do_action walk Back2 2
sq_camera addset sprung 0 {{ s 0.1 0.2} { s 0.2 0.6} { s 0.8 1.0} { s 1.0 0.0}}
sq_camera selset sprung
do_action rotate 2 1

sq_camera move Kamera1 0.9 -0.2 -0.8 0.3

sq_wait none
do_text 0138ap 1 { hermit } 139p
do_wait time 1.0

+sq_pen move Kamera1 { -1 0 0 }
sq_wait 1
do_action walk Wiggle 0
sq_wait 2
do_action rotate 0 2
do_text 0138aq 2 { talkpangb talkpanta } 139q
do_text 0138ar 2 NegReac 139r
sq_camera fix Kamera1 1.2 -0.1 0.8
do_text 0138as 2 PosAc 139s
do_text 0138at 2 { scratchhead talkrepoa } 139t
sq_camera selset inout
do_action rotate Grammophon 2
sq_wait none
do_text 0138au 2 Auto 139u { 20 20 }
do_wait time 2
sq_camera move Grammophon 0.7 -0.05 0.45 0.2
sq_wait 2
do_wait
adaptive_sound volfact 75
do_action rotate Grammophon 0
do_wait time 1
adaptive_sound volfact 100
+sq_pen move Back3 {0 0 1 }
+sq_pen move Back4 {0.5 0 1.5 }
+sq_pen move Back5 {-1 0 1 }
+sq_pen set Back1 Back2
+sq_pen move Back1 {1.5 0 -1 }
+sq_pen move Kamera1 { 0.5 0 0 }

sq_wait none
do_action walk Back3 3
do_action walk Back4 4
do_action walk Back5 5
do_wait time 2
do_action rotate Grammophon { 3 4 5 }
do_wait time 1
sq_actor actionlist 0 {}
sq_wait 0
adaptive_sound volfact 76
do_text 0138av 0 { talkacngb talkacntc } 139v

sq_wait 1
sq_actor actionlist 0 { { { anim standloopa } { anim standloopb } { anim standloopc } { anim standloopd } } loop }
sq_camera selset inout
sq_camera move Kamera1 0.9 -0.15 -0.8 1.5
adaptive_sound volfact 60
do_wait time 1.5
sq_wait 2
do_action rotate 2 0
do_text 0138aw 2 PosReac 139w
do_action rotate 0 2
+sq_pen move Kamera1 { -1 0 0 }
do_text 0138ax 2 { invent_b talkrepoa } 139x

sq_camera fix Kamera1 1.0 -0.1 0.3

do_action sneak Back1 1

sq_wait 1
do_text 0138ay 2 PosReac 139y
do_action rotate front 2
do_action rotate 2 1
do_action rotate 1 0
do_action anim invent_a 2
do_text 0138az 1 PosReac 139z
sq_camera fix Kamera1 0.9 -0.1 0.7

sq_wait 2
do_action rotate 2 0
do_text 0138ba 2 NegReac 139ba
sq_camera fix Kamera1 1.2 -0.2 -0.4
do_text 0138bb 2 Auto 139bb
do_action rotate left 2
sq_camera fix Kamera1 0.8 -0.1 0.4
do_text 0138bc 2 { invent_b invent_a } 139bc
do_text 0138bd 2 { scratchhead invent_a } 139bd

sq_wait 1
sq_actor actionlist 2 { { anim invent_a } loop }
do_action rotate 0.8 1
#do_text 0138bk 2 { invent_a invent_a } 139bk

sq_wait none
do_text 0138bg 1 { showleft talkrepoc talkrepob} 139bg
do_wait time 3
sq_camera move Kamera2 0.8 0.05 -0.3
sq_wait 1
do_wait

# ZEIGT ZUR MASCHINE, DORT LEUCHTET ETWAS AUF

sq_wait 1
do_text 0138bf 1 PosReac 139bf
do_wait time 0.5
sq_wait none
do_text 0138bl 2 { invent_a invent_a } 139bl
do_wait time 1
sq_camera fix Kamera1 1.1 -0.2 0.7
do_wait time 7
sq_camera fix 0 0.7 -0.1 -0.5
do_wait time 2
sq_actor actionlist 0 {}
sq_wait 0
sq_actor actionlist 2 { { anim invent_brains } loop }
sq_actor actionlist 1 { { { anim talkrepob } { anim standloopc } { anim standloopb } { anim talkrepob } } loop }
do_action anim standloopa { 1 2 }
do_wait time 1
do_action rotate 5.8 0
do_text 0138bh 0 NegAc 139bh
do_text 0138bi 0 NegReac 139bi
do_text 0138bj 0 Auto 139bj
do_action rotate right 0
+sq_pen move Wiggle { 1 0 1 }
do_action anim insane 0
+sq_camera fix Wiggle 1.2 -0.1 -0.5
do_action walk Wiggle 0
+do_wait time 2
+do_action beam Wiggle 0
+do_action beam Back1 1
+do_action beam Back2 2
+do_action beam Back3 3
+do_action beam Back4 4
+do_action beam Back5 5
do_wait time 1
+sq_camera get
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound volfact 100
+adaptive_sound changethemenow brains
#-----------------------------------------
+cancel_fade
+do_wait




