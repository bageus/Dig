sq_audio open 2010
sq_text file Tutorial
+sq_actor find Zwerg 40 1 0
+sq_pen set Position 0
+sq_pen setz Position 13
+sq_camera move Position 0.8 -0.2 0.3
sq_wait 0
+do_action walk Position 0
+sq_pen move Position {0.8 0 0}
+sq_pen set BeamInPos 0
+sq_pen move BeamInPos {4 0 0}
+do_action beam BeamInPos 1
sq_wait 0
+sq_pen move Position {0.7 0 -1.5}

+set inv_liste [inv_list [Actor 1]]; foreach inv_item $inv_liste if {[get_objclass $inv_item] == "Feuerstelle" } { inv_rem [Actor 1] $inv_item; set_pos $inv_item [parse_pos BeamInPos] }

sq_actor find Feuerstelle 10 1 any BeamInPos

+call_method [Actor 2] packtobox

+link_obj [Actor 2] [Actor 1] 0
+do_action transport Position 1
sq_wait 0
do_action anim teeter_w 0
do_action anim stretch 0
do_action anim wait 0


+do_action rotate left 0
do_action anim wipenose 0

sq_wait 1
+sq_pen set KistePos 1
+sq_pen move KistePos {-0.55 0 0.0}

do_action anim putboxa 1
+link_obj [Actor 2];set_pos [Actor 2] [parse_pos KistePos]

do_action anim putboxb 1

sq_wait 0
do_action anim scratch 0
do_action anim wait 0

+sq_pen move Position {-0.7 0 1.5}
+do_action walk Position 1

do_action anim teeter_w 0

sq_wait all

do_action anim breathe 0
+do_action rotate left 1
sq_wait none
do_action anim puncha 1
sq_wait all
do_action anim standbackhitl 0
sq_wait none
+sq_pen move Position {-0.9 0 -0.9}
+do_action walk Position 1
sq_wait all
+do_action rotate front 0
sq_wait none
+sq_pen move Position {-0.6 0 0.9}
#sq_pen move Position {-1 0 0.9}
+do_action walk Position 1
sq_wait all
+do_action rotate right 0
sq_wait none
do_action anim scout 0
sq_wait all
+do_action rotate right 1
sq_wait none
do_action anim mann.treten_hintern 1
#do_action anim kickmachine 1
sq_wait all
do_action anim standbackhith 0
+do_action rotate left 0
sq_wait none
do_action anim washface 0
sq_wait all
do_text 2010a 1 {talkk talka} Du_spielst
do_text 2010b 1 {talkb talkp talkf} Typisch_Ole
sq_wait none
do_action anim teeter_t 0
sq_wait all
do_text 2010c 1 {talka talkq talkc} Lass_uns
do_text 2010d 0 {talka talkq } Na_wenn
#+sq_object delete all
