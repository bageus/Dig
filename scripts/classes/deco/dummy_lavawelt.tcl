//# STOPIFNOT FULL
call scripts/init/animinit.tcl


def_class Dummy_Fenrishoehle_b none dummy 1 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim fenrishoehle_b.standard
	class_viewinfog 1
	class_particle 0 {0.55 -9.11 -19.55} {0 -.03 0} 64 2 0 0 4
	class_particle 0 {1.55 -7.81 -20.21} {0 -.04 0} 64 2 0 0 4
	class_particle 0 {-0.44 -7.81 -20.21} {0 -.02 0} 64 2 0 0 4
	class_particle 0 {1.76 -2.67 -18.15} {0 -.035 0} 64 2 0 0 4
	class_particle 0 {-0.65 -2.67 -18.15} {0 -.02 0} 64 2 0 0 4
	class_particle 0 {2.54 -2.34 -11.34} {0 -.03 0} 64 2 0 0 4
	class_particle 0 {-1.43 -2.34 -11.34} {0 -.035 0} 64 2 0 0 4
	
}





def_class Dummy_Fenrishoehle_b_w none dummy 1 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim fenrishoehle_b_w.standard
	class_viewinfog 1
	class_particle 21 {0.55 -9.11 -19.55} {0 -.03 0} 64 2 0 0 4
	class_particle 21 {1.55 -7.81 -20.21} {0 -.04 0} 64 2 0 0 4
	class_particle 21 {-0.44 -7.81 -20.21} {0 -.02 0} 64 2 0 0 4
	class_particle 21 {1.76 -2.67 -18.15} {0 -.035 0} 64 2 0 0 4
	class_particle 21 {-0.65 -2.67 -18.15} {0 -.02 0} 64 2 0 0 4
	class_particle 21 {2.54 -2.34 -11.34} {0 -.03 0} 64 2 0 0 4
	class_particle 21 {-1.43 -2.34 -11.34} {0 -.035 0} 64 2 0 0 4
	
}


SetDummyClassesAll {} {


// { Lava_wand_j lava_wand_j }

{ Lava_seule_a lava_seule_a }
{ Lava_seule_b lava_seule_b }

{ Lava_seule_d lava_seule_d }
{ Lava_seule_e lava_seule_e }
{ Lava_seule_f lava_seule_f }
{ Lava_seule_g lava_seule_g }
{ Lava_seule_h lava_seule_h }
{ Lava_seule_i lava_seule_i }
{ Lava_seule_j lava_seule_j }
{ Lava_seule_k lava_seule_k }
// { Lava_seule_l lava_seule_l }


{ Lava_abschluss_a lava_abschluss_a }
{ Lava_abschluss_b lava_abschluss_b }
{ Lava_abschluss_c lava_abschluss_c }

{ Lava_auffangbecken lava_auffangbecken anim }
{ Lava_fluss_a lava_fluss_a anim }
{ Lava_fluss_b lava_fluss_b anim }
{ Lava_fluss_c lava_fluss_c anim }
{ Lava_rinne lava_rinne anim }
{ Lava_rinneabschluss_a lava_rinneabschluss_a }
{ Lava_rinneabschluss_a1 lava_rinneabschluss_a1 }
{ Lava_rinneabschluss_b lava_rinneabschluss_b }
{ Lava_rinneabschluss_b1 lava_rinneabschluss_b1 }
{ Lava_rinneabschluss_c lava_rinneabschluss_c }
{ Lava_rinneabschluss_c1 lava_rinneabschluss_c1 }
{ Lava_speier lava_speier }

{ Lava_becken_a lava_becken_a anim }
{ Lava_becken_b lava_becken_b }
{ Lava_becken_c lava_becken_c anim }

{ Lava_krater_a lava_krater_a }
{ Lava_krater_b lava_krater_b }
{ Lava_krater_c lava_krater_c }
{ Lava_krater_d lava_krater_d }


{ Lava_moos_hm_a lava_moos_hm_a }
{ Lava_moos_hm_b lava_moos_hm_b }
{ Lava_moos_hm_c lava_moos_hm_c }
{ Lava_moos_hm_d lava_moos_hm_d }
{ Lava_moos_hm_e lava_moos_hm_e }
// { Lava_moos_oh_a lava_moos_oh_a }
// { Lava_moos_oh_b lava_moos_oh_b }
// { Lava_moos_oh_c lava_moos_oh_c }
{ Lava_moos_oh_d lava_moos_oh_d }
{ Lava_moos_oh_e lava_moos_oh_e }
{ Lava_moos_ov_a lava_moos_ov_a }
{ Lava_moos_ov_b lava_moos_ov_b }
// { Lava_moos_ov_c lava_moos_ov_c }
{ Lava_moos_ov_d lava_moos_ov_d }
// { Lava_moos_ov_e lava_moos_ov_e }
{ Lava_moos_uh_a lava_moos_uh_a }
{ Lava_moos_uh_b lava_moos_uh_b }
{ Lava_moos_uh_c lava_moos_uh_c }
{ Lava_moos_uh_d lava_moos_uh_d }
{ Lava_moos_uh_e lava_moos_uh_e }
{ Lava_moos_uh_f lava_moos_uh_f }
{ Lava_moos_uh_g lava_moos_uh_g }
// { Lava_moos_uh_h lava_moos_uh_h }
{ Lava_moos_um_a lava_moos_um_a }
{ Lava_moos_um_b lava_moos_um_b }
{ Lava_moos_um_c lava_moos_um_c }
{ Lava_moos_um_d lava_moos_um_d }
// { lava_moos_um_e lava_moos_um_e }
{ Lava_moos_um_f lava_moos_um_f }
{ Lava_moos_um_g lava_moos_um_g }
// { Lava_moos_um_h lava_moos_um_h }
{ Lava_moos_um_i lava_moos_um_i }
{ Lava_moos_um_j lava_moos_um_j }
// { Lava_moos_um_k lava_moos_um_k }
// { Lava_moos_um_l lava_moos_um_l }
// { Lava_moos_um_m lava_moos_um_m }
// { Lava_moos_um_n lava_moos_um_n }
{ Lava_moos_um_o lava_moos_um_o }
// { Lava_moos_um_p lava_moos_um_p }
{ Lava_moos_um_q lava_moos_um_q }
{ Lava_moos_uv_a lava_moos_uv_a }
{ Lava_moos_uv_b lava_moos_uv_b }
{ Lava_moos_uv_c lava_moos_uv_c }
{ Lava_moos_uv_d lava_moos_uv_d }
{ Lava_moos_uv_e lava_moos_uv_e }
// { Lava_moos_uv_f lava_moos_uv_f }
// { Lava_moos_uv_g lava_moos_uv_g }
// { Lava_moos_uv_h lava_moos_uv_h }
// { Lava_moos_uv_i lava_moos_uv_i }
{ Lava_moos_uv_j lava_moos_uv_j }
{ Lava_moos_uv_k lava_moos_uv_k }
// { Lava_moos_uv_l lava_moos_uv_l }
{ Lava_moos_uv_m lava_moos_uv_m }
{ Lava_moos_uv_n lava_moos_uv_n }
{ Lava_moos_uv_o lava_moos_uv_o }
{ Lava_moos_uv_p lava_moos_uv_p }
{ Lava_moos_uv_q lava_moos_uv_q }
{ Lava_moos_uv_r lava_moos_uv_r }
{ Lava_alienkopf lava_alienkopf }

// { Lava_rad_a lava_rad_a }
{ Lava_rad_b lava_rad_b }
{ Lava_rad_c lava_rad_c }
// { Lava_vamp_seule_a lava_vamp_seule_a }
// { Lava_vamp_seule_b lava_vamp_seule_b }
// { Lava_vamp_seule_c lava_vamp_seule_c }
// { Lava_vamp_seule_d lava_vamp_seule_d }
// { Lava_vamp_seule_e lava_vamp_seule_e }
// { Lava_vamp_seule_f lava_vamp_seule_f }
//{ Vamp_zinnen_test vamp_zinnen_test }
{ Lava_zinnen_a lava_zinnen_a }
{ Lava_zinnen_b lava_zinnen_b }
{ Lava_zinnen_c lava_zinnen_c }
{ Lava_zinnen_d lava_zinnen_d }
{ Lava_zinnen_e lava_zinnen_e }
{ Lava_zinnen_f lava_zinnen_f }
{ Lava_zinnen_g lava_zinnen_g }
{ Lava_zinnen_h lava_zinnen_h }
// { Lava_zinnen_i lava_zinnen_i }
{ Lava_zinnen_j lava_zinnen_j }
{ Lava_zinnen_k lava_zinnen_k }
// { Lava_zinnen_l lava_zinnen_l }
// { Lava_zinnen_m lava_zinnen_m }
{ Lava_zinnen_n lava_zinnen_n }
// { Lava_zinnen_p lava_zinnen_p }
{ Lava_zinnen_q lava_zinnen_q }
// { Lava_zinnen_r lava_zinnen_r }
{ Lava_zinnen_s lava_zinnen_s }

{ Lava_krst_uv_a lava_krst_uv_a }
{ Lava_krst_uv_b lava_krst_uv_b }
{ Lava_krst_uv_c lava_krst_uv_c }
{ Lava_krst_uv_d lava_krst_uv_d }
{ Lava_krst_uv_e lava_krst_uv_e }
{ Lava_krst_uv_f lava_krst_uv_f }
{ Lava_krst_uv_g lava_krst_uv_g }
{ Lava_krst_uv_h lava_krst_uv_h }
{ Lava_krst_uv_i lava_krst_uv_i }
{ Lava_krst_uv_j lava_krst_uv_j }
{ Lava_krst_uv_k lava_krst_uv_k }
{ Lava_krst_uv_l lava_krst_uv_l }
{ Lava_krst_uv_m lava_krst_uv_m }

{ Lava_krst_ov_a lava_krst_ov_a }
{ Lava_krst_ov_b lava_krst_ov_b }
// { Lava_krst_ov_c lava_krst_ov_c }
{ Lava_krst_ov_d lava_krst_ov_d }
{ Lava_krst_ov_e lava_krst_ov_e }
{ Lava_krst_ov_f lava_krst_ov_f }
{ Lava_krst_ov_g lava_krst_ov_g }
{ Lava_krst_ov_h lava_krst_ov_h }
{ Lava_krst_ov_i lava_krst_ov_i }
{ Lava_krst_ov_j lava_krst_ov_j }
{ Lava_krst_ov_k lava_krst_ov_k }
{ Lava_krst_ov_l lava_krst_ov_l }
{ Lava_krst_ov_m lava_krst_ov_m }

{ Lava_wand_vampy_a lava_wand_vampy_a }
{ Lava_wand_vampy_b lava_wand_vampy_b }
// { Lava_wand_vampy_c lava_wand_vampy_c }
// { Lava_wand_vampy_d lava_wand_vampy_d }
{ Lava_wand_vampy_e lava_wand_vampy_e }
{ Lava_wand_vampy_f lava_wand_vampy_f }
// { Lava_wand_vampy_g lava_wand_vampy_g }
// { Lava_wand_vampy_h lava_wand_vampy_h }
{ Lava_wand_vampy_i lava_wand_vampy_i }
{ Lava_wand_vampy_j lava_wand_vampy_j }
{ Lava_wand_vampy_k lava_wand_vampy_k }
{ Lava_wand_vampy_l lava_wand_vampy_l }
// { Lava_wand_vampy_m lava_wand_vampy_m }
{ Lava_wand_vampy_n lava_wand_vampy_n }
{ Lava_wand_vampy_o lava_wand_vampy_o }
{ Lava_wand_vampy_p lava_wand_vampy_p }
{ Lava_wand_vampy_q lava_wand_vampy_q }
{ Lava_wand_vampy_r lava_wand_vampy_r }
{ Lava_wand_vampy_s lava_wand_vampy_s }
{ Lava_wand_vampy_t lava_wand_vampy_t }
// { Lava_wand_vampy_u lava_wand_vampy_u }
// { Lava_wand_vampy_v lava_wand_vampy_v }
{ Lava_wand_vampy_w lava_wand_vampy_w }
{ Lava_wand_vampy_x lava_wand_vampy_x }
{ Lava_wand_vampy_y lava_wand_vampy_y }
{ Lava_wand_vampy_z lava_wand_vampy_z }
{ Lava_wand_vampy_aa lava_wand_vampy_aa }
{ Lava_wand_vampy_ab lava_wand_vampy_ab }
{ Lava_wand_vampy_ac lava_wand_vampy_ac }
{ Lava_wand_vampy_ad lava_wand_vampy_ad }
{ Lava_wand_vampy_ae lava_wand_vampy_ae }
{ Lava_wand_vampy_af lava_wand_vampy_af }
{ Lava_wand_vampy_ag lava_wand_vampy_ag }
{ Lava_wand_vampy_ah lava_wand_vampy_ah }
// { Lava_wand_vampy_ai lava_wand_vampy_ai }
// { Lava_wand_vampy_aj lava_wand_vampy_aj }
// { Lava_wand_vampy_ak lava_wand_vampy_ak }
// { Lava_wand_vampy_al lava_wand_vampy_al }
// { Lava_wand_vampy_am lava_wand_vampy_am }
// { Lava_wand_vampy_an lava_wand_vampy_an }
// { Lava_wand_vampy_ao lava_wand_vampy_ao }
// { Lava_wand_vampy_ap lava_wand_vampy_ap }
{ Lava_wand_vampy_aq lava_wand_vampy_aq }
{ Lava_wand_vampy_ar lava_wand_vampy_ar }
{ Lava_wand_vampy_as lava_wand_vampy_as }
// { Lava_wand_vampy_at lava_wand_vampy_at }
// { Lava_wand_vampy_au lava_wand_vampy_au }
// { Lava_wand_vampy_av lava_wand_vampy_av }
// { Lava_wand_vampy_aw lava_wand_vampy_aw }
// { Lava_wand_vampy_ax lava_wand_vampy_ax }
// { Lava_wand_vampy_ay lava_wand_vampy_ay }
// { Lava_wand_vampy_az lava_wand_vampy_az }
// { Lava_wand_vampy_ba lava_wand_vampy_ba }
{ Lava_wand_vampy_bb lava_wand_vampy_bb }


// { Lave_seule2_a lave_seule2_a }
// { Lave_seule2_b lave_seule2_b }
// { Lave_seule2_c lave_seule2_c }

// { Lava_balken_a lava_balken_a }
// { Lava_balken_b lava_balken_b }
{ Lava_balken_c lava_balken_c }
{ Abschluss_lava_vampy_a abschluss_lava_vampy_a }
{ Abschluss_lava_vampy_b abschluss_lava_vampy_b }
{ Abschluss_lava_vampy_c abschluss_lava_vampy_c }
{ Ecke_lava_vampy_a ecke_lava_vampy_a }
// { Ecke_lava_vampy_b ecke_lava_vampy_b }
{ Ecke_lava_vampy_c ecke_lava_vampy_c }

{ Lava_krst_h_a lava_krst_h_a }
{ Lava_krst_h_b lava_krst_h_b }
{ Lava_krst_h_c lava_krst_h_c }
{ Lava_krst_h_d lava_krst_h_d }
{ Lava_krst_h_e lava_krst_h_e }
{ Lava_krst_h_f lava_krst_h_f }
{ Lava_krst_h_g lava_krst_h_g }
{ Lava_krst_h_h lava_krst_h_h }
{ Lava_krst_h_i lava_krst_h_i }

{ Lava_krst_um_a lava_krst_um_a }
{ Lava_krst_um_b lava_krst_um_b }
{ Lava_krst_um_c lava_krst_um_c }
{ Lava_krst_um_d lava_krst_um_d }
{ Lava_krst_um_e lava_krst_um_e }
{ Lava_krst_um_f lava_krst_um_f }
{ Lava_krst_um_g lava_krst_um_g }
{ Lava_krst_um_h lava_krst_um_h }

{ Lava_krst_o_a lava_krst_o_a }
{ Lava_krst_o_b lava_krst_o_b }
{ Lava_krst_o_c lava_krst_o_c }
{ Lava_krst_o_d lava_krst_o_d }
{ Lava_krst_o_e lava_krst_o_e }
{ Lava_krst_o_f lava_krst_o_f }
{ Lava_krst_o_g lava_krst_o_g }
{ Lava_krst_o_h lava_krst_o_h }
{ Lava_krst_o_i lava_krst_o_i }
{ Lava_krst_o_j lava_krst_o_j }
{ Lava_krst_o_k lava_krst_o_k }
{ Lava_krst_o_l lava_krst_o_l }
{ Lava_krst_o_m lava_krst_o_m }
{ Lava_krst_o_n lava_krst_o_n }
{ Lava_krst_o_o lava_krst_o_o }
{ Lava_krst_o_p lava_krst_o_p }

{ Lava_gargoyle lava_gargoyle }
{ Lava_vulkan_a lava_vulkan_a }
{ Lava_vulkan_b lava_vulkan_b }
{ Lava_vulkan_c lava_vulkan_c }
{ Lava_vulkan_lava_a lava_vulkan_lava_a anim }
{ Lava_vulkan_lava_b lava_vulkan_lava_b anim }
{ Lava_vulkan_lava_c lava_vulkan_lava_c anim }
{ Lava_vulkan_quelle lava_vulkan_quelle anim }
// { Lava_amboss lava_amboss anim }
// { Lava_amboss_b lava_amboss_b }
{ Lava_fenster lava_fenster }
{ Lava_fenster_b lava_fenster_b }

{ Fenrishoehle_a fenrishoehle_a }	
{ Fenrishoehle_a_2 fenrishoehle_a_2 }
{ Fenrishoehle_a_3 fenrishoehle_a_3 }
{ Fenrishoehle_a_lava fenrishoehle_a_lava anim }
{ Fenrishoehle_a_quelle fenrishoehle_a_quelle anim }


{ Fenrishoehle_b_2 fenrishoehle_b_2 }
{ Fenrishoehle_b_3 fenrishoehle_b_3 }
{ Fenrishoehle_b_lava fenrishoehle_b_lava anim }
{ Fenrishoehle_b_quelle fenrishoehle_b_quelle  anim}


{ Fenrishoehle_b_lava_w fenrishoehle_b_lava_w anim }
{ Fenrishoehle_b_quelle_w fenrishoehle_b_quelle_w  anim}

{ Fenrishoehle_c fenrishoehle_c }

{ Abschluss_lava_a abschluss_lava_a }
{ Abschluss_lava_b abschluss_lava_b }
{ Ecke_lava ecke_lava }
{ Fenris_karte fenris_karte }
// { Fenris_stuhl fenris_stuhl }
// { Lava_hammer_stein lava_hammer_stein }

// { Lava_tuer lava_tuer }

{ Lava_brueckenteil_a lava_brueckenteil_a anim}
{ Lava_brueckenteil_b lava_brueckenteil_b anim}
{ Lava_4thring_tor lava_4thring_tor }

// { Lava_flammenwerfer lava_flammenwerfer }

// { Lava_schalter_a lava_schalter_a }
{ Lava_lostvegos lava_lostvegos }


}

SetDummyClassesNoZ {

{ Boden_lava_vampy_a boden_lava_vampy_a }
{ Boden_lava_vampy_b boden_lava_vampy_b }
{ Boden_lava_vampy_c boden_lava_vampy_c }
{ Boden_lava_vampy_d boden_lava_vampy_d }
{ Boden_lava_vampy_e boden_lava_vampy_e }
{ Boden_lava_vampy_f boden_lava_vampy_f }
{ Boden_lava_vampy_g boden_lava_vampy_g }
{ Boden_lava_vampy_h boden_lava_vampy_h }
{ Boden_lava_vampy_i boden_lava_vampy_i }
{ Boden_lava_vampy_j boden_lava_vampy_j }
{ Boden_lava_vampy_k boden_lava_vampy_k }
{ Boden_lava_vampy_l boden_lava_vampy_l }
{ Boden_lava_vampy_m boden_lava_vampy_m }
{ Boden_lava_vampy_n boden_lava_vampy_n }
{ Boden_lava_vampy_o boden_lava_vampy_o }
{ Boden_lava_vampy_p boden_lava_vampy_p }

{ Boden_lava_vampy_r boden_lava_vampy_r }
{ Boden_lava_vampy_s boden_lava_vampy_s }
{ Boden_lava_vampy_t boden_lava_vampy_t }
{ Boden_lava_vampy_u boden_lava_vampy_u }



	

}


