call scripts/init/animinit.tcl


def_class Dummy_Feuerkelch_a none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_viewinfog 1
	class_physic 0
	class_light {0 -0.5 0.7} 2 {0.7 0.6 0.4}
	class_particle 2 {0 -0.45 0.4} {0 0 0} 64 2 0 0 0
	class_defaultanim feuerkelch_a.standard
}

def_class Dummy_Feuerkelch_b none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_viewinfog 1
	class_physic 0
	class_light {0 -0.5 0.7} 2 {0.7 0.6 0.4}
	class_particle 2 {0 -0.2 0.6} {0 0 0} 64 2 0 0 0
	class_defaultanim feuerkelch_b.standard
}

def_class Dummy_Haengelicht none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_physcategory 2
	class_viewinfog 1
	class_physic 0
	class_light {0 1.5 0} 4.5 {0.7 0.5 0.4}
	class_particle 3 {0 1.6 0} {0 0 0} 128 4 0 0 0
	class_defaultanim haengelicht.standard
}

def_class Dummy_Stehlanglicht none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_viewinfog 1
	class_physic 1
	class_light {0 -2 0} 5 {0.8 0.6 0.4}
	class_defaultanim stehlanglicht.standard
	class_particle 3 {0 -2.1 0} {0 0 0} 128 4 0 0 0
}

def_class Dummy_Trohnmonster none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_viewinfog 1
	class_physic 1
	class_light {0 -2.25 5} 6 {0.7 0.5 0.4}
	class_particle 3 {-2.8 -2.25 0.5} {0 0 0} 512 16 0 0 0
	class_particle 3 {2.8 -2.15 0.3} {0 0 0} 512 16 0 0 0
	class_defaultanim trohnmonster.standard
	class_collision 1
}

def_class Dummy_Tischlicht none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_viewinfog 1
	class_physic 1
	class_light {0 -0.5 0} 1 {0.6 0.6 0.4}
	class_defaultanim tischlicht.standard $ANIM_LOOP
	class_disablescripting
}



SetFrontDummyClassesNoPhys {
{ Armknochen armknochen }
{ Balken_a balken_a }
{ Balken_b balken_b }
{ Balken_c balken_c }
{ Balken_d balken_d }
{ Balken_e balken_e }
{ Beinknochen beinknochen }
{ Bierbecher bierbecher }
{ Bierfass bierfass }
// { Bombe bombe }
// { Bombenregal bombenregal }
{ Braten_a braten_a }
// { Buechse buechse }
{ Eimer_a eimer_a }
{ Eimer_b eimer_b }
{ Eisernejungfrau eisernejungfrau }
{ Fahne_a fahne_a }
{ Fahne_b fahne_b }
{ Fahne_c fahne_c }
{ Fahne_d fahne_d }
{ Fahne_e fahne_e }
{ Fahne_f fahne_f }
{ Fahne_g fahne_g }
{ Gloecknerscheibe_a gloecknerscheibe_a }
// { Hamstervorleger hamstervorleger}
{ Wukervorleger wukervorleger}
{ Holzkralle holzkralle }
{ Holzstuhl_a holzstuhl_a }
{ Holzstuhl_b holzstuhl_b }
{ Holztisch_a holztisch_a }
{ Holztisch_b holztisch_b }
// { Kaefig_a kaefig_a }
{ Kaefig_b kaefig_b }
{ Kaefig_c kaefig_c }
// { Kanokugel_a kanokugel_a }
// { Kanokugel_b kanokugel_b }
{ Kanone kanone }
{ Kassentisch kassentisch }
{ Kram_a kram_a }
{ Kram_b kram_b }
{ Kram_c kram_c }
{ Kram_d kram_d }
// { Kram_e kram_e }
// { Kram_f kram_f }

{ Kram_i kram_i }
{ Kram_j kram_j }
{ Kram_k kram_k }
{ Krug_a krug_a }
{ Metallschild_a metallschild_a }
// { Metallschild_b metallschild_b }
// { Metallschild_c metallschild_c }
{ Metallschild_d metallschild_d }
// { Metallschild_e metallschild_e }
{ Metallschild_f metallschild_f }
// { Metallschild_g metallschild_g }
{ Metallschild_h metallschild_h }
{ Metallschild_i metallschild_i }
// { Metallschild_j metallschild_j }
{ Metallschild_k metallschild_k }
// { Metallschild_l metallschild_l }
// { Metallschild_m metallschild_m }
{ Metallschild_n metallschild_n }
// { Metallschild_o metallschild_o }
// { Metallschild_p metallschild_p }
{ Metallschild_q metallschild_q }
// { Metawa_a metawa_a }
// { Metawa_b metawa_b }
// { Metawa_c metawa_c }
// { Metawa_d metawa_d }
// { Metawa_e metawa_e }
// { Metawa_f metawa_f }
// { Metawa_g metawa_g }
{ Metawa_h metawa_h }
{ Metawa_i metawa_i }
// { Metawa_j metawa_j }
// { Metawa_k metawa_k }
// { Metawa_l metawa_l }
{ Metawa_m metawa_m }
{ Metawa_n metawa_n }
// { Metawa_o metawa_o }
// { Metawa_p metawa_p }
// { Metawa_q metawa_q }
{ Metawa_r metawa_r }
// { Metawa_s metawa_s }
{ Metawa_t metawa_t }
// { Metawa_u metawa_u }
// { Metawa_v metawa_v }
{ Metawa_w metawa_w }
{ Metawa_x metawa_x }
// { Pulverfass pulverfass }
{ Rumpfknochen rumpfknochen }
// { Schleifstein schleifstein }
{ Seule_m_a seule_m_a }
// { Seule_m_b seule_m_b }
{ Seule_o_a seule_o_a }
// { Seule_o_b seule_o_b }
{ Seule_o_c seule_o_c }
{ Seule_u_a seule_u_a }
// { Seule_u_b seule_u_b }
{ Seule_u_c seule_u_c }
{ Stoff_a stoff_a }
{ Stoff_b stoff_b }
{ Stoff_c stoff_c }
{ Stoff_d stoff_d }
{ Stoff_e stoff_e }
{ Streckbank_a streckbank_a }
{ T_traeger_a t_traeger_a }
{ Teller_a teller_a }
{ Traeger_a traeger_a }
{ Traeger_b traeger_b }
// { Traeger_c traeger_c }
// { Traeger_d traeger_d }
{ Traeger_e traeger_e }
// { Traeger_f traeger_f }
// { Traeger_g traeger_g }
{ Traeger_h traeger_h }
{ Traeger_i traeger_i }
{ Traeger_j traeger_j }
{ Traeger_k traeger_k }
{ Trollbett_a trollbett_a }
{ Trollbett_b trollbett_b }
{ Trollbett_c trollbett_c }
{ Trophaee_a trophaee_a }
// { Trophaee_b trophaee_b }
{ Trophaee_c trophaee_c }
// { Tuer_kaserne tuer_kaserne }
// { Tuer_tempel tuer_tempel }
// { Tuer_verlies tuer_verlies }
// { Tuer_wohn tuer_wohn }
{ Turm turm }
{ Waffenregal waffenregal }
{ Waffenstaender waffenstaender }
{ Wecker wecker }
{ Zwergengerippe zwergengerippe }
{ Zwergengerippe_b zwergengerippe_b }
{ Zwergengerippe_c zwergengerippe_c }
{ Gitter_a gitter_a }
{ Gitter_b gitter_b }
{ Bespannung_a bespannung_a }
{ Bespannung_b bespannung_b }
// { Bespannung_c bespannung_c }
{ Holzstange_a holzstange_a }
{ Holzstange_b holzstange_b }

{ Regal regal }
{ Schrank_a schrank_a }
{ Schrank_b schrank_b }
{ Schrank_c schrank_c }
{ Holztisch holztisch }
{ Holzstuhl holzstuhl }
// { Holzbett holzbett }
{ Toilette toilette }
{ Seule_o_d seule_o_d }
{ Seule_o_e seule_o_e }
// { Seule_o_f seule_o_f }
{ Seule_o_g seule_o_g }
{ Seule_u_d seule_u_d }
{ Seule_m_e seule_m_e }
{ Seule_m_f seule_m_f }
{ Seule_m_g seule_m_g }

{ Spielkarten_a spielkarten_a }
// { Spielkarten_b spielkarten_b }
{ Spielkarten_c spielkarten_c }
{ Troll_zinnen_a troll_zinnen_a }
{ Troll_zinnen_b troll_zinnen_b }
{ Troll_zinnen_c troll_zinnen_c }
{ Troll_zinnen_d troll_zinnen_d }
{ Troll_zinnen_e troll_zinnen_e }
{ Troll_zinnen_f troll_zinnen_f }
{ Troll_zinnen_g troll_zinnen_g }
{ Troll_zinnen_h troll_zinnen_h }
{ Troll_zinnen_i troll_zinnen_i }
{ Troll_zinnen_j troll_zinnen_j }
{ Troll_zinnen_k troll_zinnen_k }
{ Troll_zinnen_l troll_zinnen_l }
{ Troll_zinnen_m troll_zinnen_m }



}
