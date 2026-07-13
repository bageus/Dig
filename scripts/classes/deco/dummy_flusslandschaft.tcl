call scripts/init/animinit.tcl



def_class Dummy_Urw_wasserfall_a none dummy 0 {} {

	method let_me_be_an_obj {} {}

	class_viewinfog 1
	class_physic 1
	class_light {1.3 -2.2 2.5} 8 {0 0 0.4}
	class_defaultanim urw_wasserfall_a.standard 2

	class_particle 21 {-1.5 6 -2.8} {0 -0.05 0.1} 16 5 0 0 2
	class_particle 21 {1.5 6 -2.8} {0 -0.05 0.1} 17 4 0 0 2
	class_particle 21 {0.65 6 -1.6} {0 -0.05 0.1} 16 3 0 0 2
	class_particle 21 {-0.65 6 -1.6} {0 -0.05 0.1} 15 4 0 0 2
	
//	class_particle 12 {-0.65 5.5 0.75} {0 0 0} 16 4 0 0 10

//	class_particle 24 {0 -2 0} {0 -0.15 3} 32 1 0 0 2
//	class_particle 22 {0 0 0} {0 0 0} 32 1 0 0 2
//	class_particle 21 {0 2 0} {0 0 3} 32 1 0 0 2
}

def_class Dummy_Leuchtblume_boden none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {1.3 -2.2 2.5} 4 {0.6 0.45 0.4}
	class_defaultanim leuchtblume_boden.standard
	class_disablescripting
}

def_class Dummy_Leuchtblume_root none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {0 0.3 1.5} 4 {0.35 0.4 0.2}
	class_defaultanim leuchtblume_root.standard
	class_disablescripting
}

def_class Dummy_Leuchtblume_wand none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {1.3 1.9 5} 4 {0.3 0.45 0.5}
	class_defaultanim leuchtblume_wand.standard
	class_disablescripting
}


def_class Dummy_Baumpilz_a none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {0 1.8 0} 4 {0.4 0.4 0.3}
	class_defaultanim baumpilz_a.standard
	class_disablescripting
}

def_class Dummy_Baumpilz_b none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {0 1.8 0} 4 {0.25 0.3 0.25}
	class_defaultanim baumpilz_b.standard
	class_disablescripting
}

def_class Dummy_Baumpilz_c none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {0 1.8 0} 4 {0.3 0.3 0.3}
	class_defaultanim baumpilz_c.standard
	class_disablescripting
}

def_class Dummy_Baumpilz_d none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {0 1.8 0} 4 {0.25 0.25 0.3}
	class_defaultanim baumpilz_d.standard
	class_disablescripting
}

def_class Dummy_Baumpilz_e none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {0 1.8 0} 4 {0.25 0.25 0.3}
	class_defaultanim baumpilz_e.standard
	class_disablescripting
}

def_class Dummy_Baumpilz_f none dummy 0 {} {
	class_viewinfog 1
	class_physic 0
	class_light {0 1.8 0} 4 {0.3 0.3 0.3}
	class_defaultanim baumpilz_f.standard
	class_disablescripting
}

def_class Dummy_Lichtpunkt_a none dummy 0 {} {
	class_viewinfog 0
	class_physic 0
	class_defaultanim lichtpunkt_a.standard 2
	def_event dummy_e
	handle_event dummy_e {}
	method dummy_m {} {}
	obj_init {set_anim this lichtpunkt_a.standard [irandom 7] 2}
}

SetFrontDummyClassesNoPhys {
{ Schlabber_a schlabber_a }
{ Schlabber_b schlabber_b }
{ Schlabber_c schlabber_c }
{ Schlabber_d schlabber_d }
{ Liane_a liane_a }
{ Liane_b liane_b }
{ Liane_c liane_c }
{ Liane_d liane_d }
{ Liane_e liane_e }
{ Liane_f liane_f }
{ Liane_g liane_g }
{ Liane_h liane_h }
{ Liane_i liane_i }
{ Liane_j liane_j }
{ Liane_k liane_k }
{ Liane_l liane_l }
// { Pflanze_a pflanze_a}
// { Pflanze_b pflanze_b}
{ Moos_hm_a moos_hm_a }
{ Moos_hm_b moos_hm_b }
{ Moos_hm_c moos_hm_c }
{ Moos_hm_d moos_hm_d }
{ Moos_hm_e moos_hm_e }
{ Moos_oh_a moos_oh_a }
// { Moos_oh_b moos_oh_b }

{ Moos_oh_d moos_oh_d }
{ Moos_oh_e moos_oh_e }
{ Moos_ov_a moos_ov_a }
{ Moos_ov_b moos_ov_b }
{ Moos_ov_c moos_ov_c }
{ Moos_ov_d moos_ov_d }
{ Moos_ov_e moos_ov_e }
{ Moos_uh_a moos_uh_a }
{ Moos_uh_b moos_uh_b }
{ Moos_uh_c moos_uh_c }
{ Moos_uh_d moos_uh_d }
{ Moos_uh_e moos_uh_e }
{ Moos_uh_f moos_uh_f }
{ Moos_uh_g moos_uh_g }
{ Moos_uh_h moos_uh_h }
{ Moos_um_a moos_um_a }
{ Moos_um_b moos_um_b }
{ Moos_um_c moos_um_c }
{ Moos_um_d moos_um_d }
{ Moos_um_e moos_um_e }
{ Moos_um_f moos_um_f }
{ Moos_um_g moos_um_g }

{ Moos_um_i moos_um_i }
{ Moos_um_j moos_um_j }
{ Moos_um_k moos_um_k }
{ Moos_um_l moos_um_l }
{ Moos_um_m moos_um_m }
{ Moos_um_n moos_um_n }
{ Moos_um_o moos_um_o }

{ Moos_um_q moos_um_q }
{ Moos_uv_a moos_uv_a }
{ Moos_uv_b moos_uv_b }
{ Moos_uv_c moos_uv_c }
{ Moos_uv_d moos_uv_d }
{ Moos_uv_e moos_uv_e }
// { Moos_uv_f moos_uv_f }
{ Moos_uv_g moos_uv_g }
{ Moos_uv_h moos_uv_h }

{ Moos_uv_m moos_uv_m }
{ Moos_uv_n moos_uv_n }
{ Moos_uv_o moos_uv_o }
{ Moos_uv_p moos_uv_p }
{ Moos_uv_q moos_uv_q }
{ Moos_uv_r moos_uv_r }
{ Krokus krokus_a }
{ Spargel_a spargel_a }
{ Spargel_b spargel_b }
{ Spargel_c spargel_c }
{ Tomblume tomblume }
{ Tomblume_b tomblume_b }
// { Langpilz_a langpilz_a }
// { Langpilz_b langpilz_b }
// { Langpilz_c langpilz_c }
// { Langpilz_d langpilz_d }
{ Langwurzelpilz_a langwurzelpilz_a }
{ Lesepilz_a lesepilz_a }
// { Morcheln_a morcheln_a }
// { Morcheln_b morcheln_b }
// { Morcheln_c morcheln_c }
// { Morcheln_d morcheln_d }
{ Pickerpilz_a pickerpilz_a }
{ Pilze_00_a pilze_00_a }
{ Pilze_00_b pilze_00_b }
{ Pilze_00_c pilze_00_c }
{ Pilze_00_d pilze_00_d }
{ Pilze_01_a pilze_01_a }
{ Pilze_01_b pilze_01_b }
{ Pilze_01_c pilze_01_c }
{ Pilze_02_a pilze_02_a }
{ Pilze_02_b pilze_02_b }
{ Pilze_02_c pilze_02_c }
{ Pilze_03_a pilze_03_a }
{ Pilze_03_b pilze_03_b }
{ Pilze_03_c pilze_03_c }
{ Pilze_04_a pilze_04_a }
{ Pilze_04_b pilze_04_b }
{ Pilze_04_c pilze_04_c }
{ Pilze_05_a pilze_05_a }
{ Pilze_05_b pilze_05_b }
{ Pilze_05_c pilze_05_c }
{ Riesenpfefferpilz_a riesenpfefferpilz_a }
// { Riesenpfefferpilz_b riesenpfefferpilz_b }
{ Riesenpfefferpilz_c riesenpfefferpilz_c }
{ Riesenpilz_a riesenpilz_a }
// { Schwammpilz_a schwammpilz_a }
// { Teerpilz_a teerpilz_a }
// { Zwergenhutpilz_a zwergenhutpilz_a }
{ Zwergenhutpilz_b zwergenhutpilz_b }
// { Zwergenhutpilz_c zwergenhutpilz_c }
{ Zwergenhutpilz_d zwergenhutpilz_d }
{ Gras_03 {gras_03_a gras_03_b gras_03_c gras_03_d}}
{ Gras_04 {gras_04_a gras_04_b gras_04_c gras_04_d}}
{ Gras_05 {gras_05_a gras_05_b gras_05_c gras_05_d}}
{ Gras_06 {gras_06_a gras_06_b gras_06_c gras_06_d}}
{ Gras_07 {gras_07_a gras_07_b gras_07_c gras_07_d}}
{ Spinweb_a spinweb_a }
{ Spinweb_b spinweb_b }
{ Spinweb_c spinweb_c }
{ Spinweb_d spinweb_d }
{ Spinweb_e spinweb_e }
{ Spinweb_f spinweb_f }
{ Spinweb_g spinweb_g }
{ Wiese_oval wiese_oval_a}
{ Wiese_rund wiese_rund_a}
{ Wurzel_00_a wurzel_00_a }
{ Wurzel_00_b wurzel_00_b }
{ Wurzel_01_a wurzel_01_a }
{ Wurzel_01_b wurzel_01_b }
{ Wurzel_02_a wurzel_02_a }
{ Wurzel_02_b wurzel_02_b }
{ Wurzel_03_a wurzel_03_a }
{ Wurzel_03_b wurzel_03_b }
{ Wurzel_04_a wurzel_04_a }
{ Wurzel_04_b wurzel_04_b }
{ Wurzel_05_a wurzel_05_a }
{ Wurzel_05_b wurzel_05_b }
{ Baumstumpf_moss_a baumstumpf_moss_a }
{ Baumstumpf_moss_b baumstumpf_moss_b }
// { Krokosklett krokosklett }

{ Stein_a stein_a }
{ Stein_b stein_b }
{ Stein_c stein_c }
{ Stein_o_a stein_o_a }
{ Stein_o_b stein_o_b }
{ Stein_o_c stein_o_c }
{ Stein_u_a stein_u_a }
{ Stein_u_b stein_u_b }
{ Stein_u_c stein_u_c }
{ Stein_z_a stein_z_a }
{ Stein_z_b stein_z_b }
{ Stein_z_c stein_z_c }
// { Hinkel_a hinkel_a }
// { Hinkel_b hinkel_b }
// { Hinkel_c hinkel_c }
{ Baumpilzbaum baumpilzbaum }

// { Bppilz_a bppilz_a }
// { Bppilz_b bppilz_b }
// { Bppilz_c bppilz_c }
// { Schalter_a schalter_a }
// { Schalter_b schalter_b }

{ Cs_pfeil_a cs_pfeil_a anim }
{ Cs_pfeil_b cs_pfeil_b anim }
{ Cs_kreuz_a cs_kreuz_a anim }
 


}

SetDummyClassesAll {noidiobj} {

{ Hoehlenmalerei_a hoehlenmalerei_a }
{ Riesentor riesentor }
{ Urw_wasserfall_b urw_wasserfall_b anim }

}

