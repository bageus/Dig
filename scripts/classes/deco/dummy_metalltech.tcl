
call scripts/init/animinit.tcl

def_class Dummy_Swf_krst_krat_a_1 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_krst_krat_a.standard
	class_particle 9 {0 0 0} {0.05 -0.15 0.1} 5 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_krst_krat_a_2 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_krst_krat_a.standard
	class_particle 9 {0 0 0} {0 0 0} 10 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_u_b_1 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_u_b.standard
	class_particle 9 {0 -0.5 0} {0 0 0} 5 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_u_b_2 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_u_b.standard
	class_particle 9 {0 -0.5 0} {0 0 0} 10 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_u_c_1 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_u_c.standard
	class_particle 9 {0 -0.85 0} {0 0 0} 5 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_u_c_2 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_u_c.standard
	class_particle 9 {0 -0.85 0} {0 0 0} 10 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_vorne_g_1 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_vorne_g.standard
	class_particle 9 {0 0 0} {0 0 0} 5 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_vorne_g_2 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_vorne_g.standard
	class_particle 9 {0 0 0} {0 0 0} 10 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_vorne_e_1 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_vorne_e.standard
	class_particle 9 {0 0 0} {0 0 0} 5 1 0 0 0
	class_viewinfog 1
}

def_class Dummy_Swf_sta_vorne_e_2 none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_defaultanim swf_sta_vorne_e.standard
	class_particle 9 {0 0 0} {0 0 0} 10 1 0 0 0
	class_viewinfog 1
}


SetFrontDummyClassesNoPhys {

 { Bogen_b bogen_b }
//{ Bogen_d bogen_d }
{ Bogen_e bogen_e }
// { Gn_01 gn_01 }
// { Gn_02 gn_02 }
// { Gn_05 gn_05 }
{ Kette_a kette_a }
{ Kette_b kette_b }
{ Metapfeiler_a metapfeiler_a }
{ Metapfeiler_b metapfeiler_b }
{ Metapfeiler_c metapfeiler_c }
{ Metapfeiler_d metapfeiler_d }

{ Rohr_a rohr_a }
{ Rohr_b rohr_b }
{ Rohr_c rohr_c }
{ Rohr_d rohr_d }
{ Rohrmini_a rohrmini_a }
{ Rohrmini_b rohrmini_b }
{ Rohrmini_c rohrmini_c }
// { Rohrmini_d rohrmini_d }
// { Rohrmini_e rohrmini_e }
// { Rohrmini_f rohrmini_f }
{ Rohrmini_g rohrmini_g }
// { Rohrmini_h rohrmini_h }
// { Rohrmini_i rohrmini_i }
// { Rohrmini_j rohrmini_j }

{ Rohrmini_m rohrmini_m }
// { Rohrmini_n rohrmini_n }
{ Rohrmini_o rohrmini_o }
{ Rohrmini_muffe_a rohrmini_muffe_a }
{ Verteiler_a verteiler_a }
{ Verteiler_b verteiler_b }


{ Wand_met_a wand_met_a }
// { Wand_met_b wand_met_b }
// { Wand_met_c wand_met_c }

// { Wand_met_e wand_met_e }
// { Wand_met_f wand_met_f }
// { Wand_met_g wand_met_g }

{ Wand_met_i wand_met_i }
{ Wand_met_kram_a wand_met_kram_a }
{ Wand_met_kram_b wand_met_kram_b }
{ Wand_met_kram_c wand_met_kram_c }

{ Wand_met_kram_h wand_met_kram_h }

{ Wand_met_blau_a wand_met_blau_a }
{ Wand_met_blau_b wand_met_blau_b }
// { Wand_met_blau_c wand_met_blau_c }
{ Wand_met_blau_d wand_met_blau_d }
{ Wand_met_blau_e wand_met_blau_e }
{ Wand_met_blau_f wand_met_blau_f }
{ Wand_met_blau_g wand_met_blau_g }
{ Wand_met_blau_h wand_met_blau_h }
{ Wand_met_blau_i wand_met_blau_i }
{ Wand_met_blau_j wand_met_blau_j }
{ Wand_met_blau_k wand_met_blau_k }
{ Wand_met_blau_l wand_met_blau_l }

{ Wand_met_blau_n wand_met_blau_n }
{ Wand_met_blau_o wand_met_blau_o }
{ Wand_met_blau_p wand_met_blau_p }
{ Wand_met_blau_q wand_met_blau_q }
{ Wand_met_blau_r wand_met_blau_r }
{ Wand_met_blau_s wand_met_blau_s }

// { Met_amatur_a met_amatur_a }
{ Met_amatur_b met_amatur_b }
{ Met_amatur_c met_amatur_c }
{ Met_amatur_d met_amatur_d }


{ Ecke_met_a ecke_met_a }
{ Wand_met_platte_a wand_met_platte_a }

{ Abschluss_met_a abschluss_met_a }
{ Abschluss_met_b abschluss_met_b }


{ Wand_met_transp_f wand_met_transp_f }


// { Wand_met_rand_a wand_met_rand_a }
// { Wand_met_rand_b wand_met_rand_b }
// { Wand_met_rand_c wand_met_rand_c }
// { Wand_met_rand_d wand_met_rand_d }
// { Wand_met_rand_e wand_met_rand_e }

{ Wand_met_rand_g wand_met_rand_g }
{ Wand_met_rand_h wand_met_rand_h }
{ Wand_met_rand_i wand_met_rand_i }


// { Wand_met_end_b wand_met_end_b }
{ Wand_met_end_c wand_met_end_c }
{ Wand_met_end_d wand_met_end_d }


// { Schleim_a schleim_a }
// { Schleim_b schleim_b }
// { Schleim_c schleim_c }
// { Schleim_d schleim_d }
// { Schleim_e schleim_e }
// { Schleim_f schleim_f }
// { Schleim_g schleim_g }

{ Met_saeule_gitter_u_a met_saeule_gitter_u_a }
{ Met_saeule_gitter_m_a met_saeule_gitter_m_a }
// { Met_saeule_gitter_m_b met_saeule_gitter_m_b }
// { Met_saeule_gitter_m_c met_saeule_gitter_m_c }
{ Met_saeule_gitter_m_d met_saeule_gitter_m_d }
{ Gelb_lore_a gelb_lore_a }
{ Gelb_lore_b gelb_lore_b }
{ Stollen_a stollen_a }
// { Stollen_b stollen_b }

// { Swf_sta_m_a swf_sta_m_a }
{ Swf_sta_m_b swf_sta_m_b }
{ Swf_sta_m_c swf_sta_m_c }
{ Swf_sta_m_d swf_sta_m_d }
// { Swf_sta_m_e swf_sta_m_e }
{ Swf_sta_m_f swf_sta_m_f }
// { Swf_sta_m_g swf_sta_m_g }
// { Swf_sta_m_h swf_sta_m_h }
// { Swf_sta_m_i swf_sta_m_i }

{ Swf_sta_m_b_big swf_sta_m_b_big }
{ Swf_sta_m_c_big swf_sta_m_c_big }
{ Swf_sta_m_d_big swf_sta_m_d_big }

{ Swf_krst_krat_a swf_krst_krat_a }
// { Swf_krst_krat_b swf_krst_krat_b }
{ Swf_krst_uv_a swf_krst_uv_a }
{ Swf_krst_uv_b swf_krst_uv_b }
{ Swf_krst_uv_c swf_krst_uv_c }
{ Swf_krst_uv_d swf_krst_uv_d }
{ Swf_krst_uv_e swf_krst_uv_e }
{ Swf_krst_uv_f swf_krst_uv_f }
// { Swf_krst_uv_g swf_krst_uv_g }
{ Swf_krst_uv_h swf_krst_uv_h }
{ Swf_krst_uv_i swf_krst_uv_i }
{ Swf_krst_uv_j swf_krst_uv_j }
{ Swf_krst_uv_k swf_krst_uv_k }
// { Swf_krst_uv_l swf_krst_uv_l }
{ Swf_krst_uv_m swf_krst_uv_m }
{ Swf_krst_uv_n swf_krst_uv_n }
{ Swf_krst_uv_o swf_krst_uv_o }
{ Swf_krst_uv_p swf_krst_uv_p }
{ Swf_krst_uv_q swf_krst_uv_q }
{ Swf_krst_uv_r swf_krst_uv_r }
{ Swf_krst_uv_s swf_krst_uv_s }
{ Swf_krst_uv_t swf_krst_uv_t }
{ Swf_krst_uv_u swf_krst_uv_u }
{ Swf_krst_uv_v swf_krst_uv_v }
{ Swf_sta_u_a swf_sta_u_a }
{ Swf_sta_u_b swf_sta_u_b }
{ Swf_sta_u_c swf_sta_u_c }
{ Swf_sta_u_d swf_sta_u_d }
{ Swf_sta_u_e swf_sta_u_e }
{ Swf_sta_u_f swf_sta_u_f }
{ Swf_sta_u_g swf_sta_u_g }
// { Swf_sta_u_h swf_sta_u_h }
// { Swf_sta_u_i swf_sta_u_i }
// { Swf_sta_u_j swf_sta_u_j }
{ Swf_sta_u_k swf_sta_u_k }
{ Swf_sta_u_l swf_sta_u_l }

{ Swf_sta_u_a_big swf_sta_u_a_big }
// { Swf_sta_u_b_big swf_sta_u_b_big }
{ Swf_sta_u_c_big swf_sta_u_c_big }
// { Swf_sta_u_d_big swf_sta_u_d_big }
// { Swf_sta_u_e_big swf_sta_u_e_big }
// { Swf_sta_u_f_big swf_sta_u_f_big }
// { Swf_sta_u_g_big swf_sta_u_g_big }
{ Swf_sta_u_h_big swf_sta_u_h_big }
{ Swf_sta_u_i_big swf_sta_u_i_big }
{ Swf_sta_u_j_big swf_sta_u_j_big }
{ Swf_sta_u_k_big swf_sta_u_k_big }
{ Swf_sta_u_l_big swf_sta_u_l_big }

{ Swf_sta_o_a swf_sta_o_a }
{ Swf_sta_o_b swf_sta_o_b }
{ Swf_sta_o_c swf_sta_o_c }
{ Swf_sta_o_d swf_sta_o_d }
// { Swf_sta_o_e swf_sta_o_e }
// { Swf_sta_o_f swf_sta_o_f }


{ Swf_sta_o_a_big swf_sta_o_a_big }
// { Swf_sta_o_b_big swf_sta_o_b_big }
{ Swf_sta_o_c_big swf_sta_o_c_big }
{ Swf_sta_o_d_big swf_sta_o_d_big }
{ Swf_sta_o_e_big swf_sta_o_e_big }
{ Swf_sta_o_f_big swf_sta_o_f_big }


{ Swf_krst_um_a swf_krst_um_a }
{ Swf_krst_um_b swf_krst_um_b }
{ Swf_krst_um_c swf_krst_um_c }
{ Swf_krst_um_d swf_krst_um_d }
{ Swf_krst_um_e swf_krst_um_e }
{ Swf_krst_um_f swf_krst_um_f }
{ Swf_krst_um_g swf_krst_um_g }

{ Swf_krst_um_i swf_krst_um_i }

{ Swf_krst_um_k swf_krst_um_k }
{ Swf_krst_um_l swf_krst_um_l }
{ Swf_krst_um_m swf_krst_um_m }

{ Swf_krst_ov_a swf_krst_ov_a }
{ Swf_krst_ov_b swf_krst_ov_b }
{ Swf_krst_ov_c swf_krst_ov_c }
// { Swf_krst_ov_d swf_krst_ov_d }
{ Swf_krst_ov_e swf_krst_ov_e }
{ Swf_krst_ov_f swf_krst_ov_f }
{ Swf_krst_ov_g swf_krst_ov_g }

// { Swf_krst_ov_a_big swf_krst_ov_a_big }
// { Swf_krst_ov_b_big swf_krst_ov_b_big }
// { Swf_krst_ov_c_big swf_krst_ov_c_big }
// { Swf_krst_ov_d_big swf_krst_ov_d_big }
// { Swf_krst_ov_e_big swf_krst_ov_e_big }
// { Swf_krst_ov_f_big swf_krst_ov_f_big }
// { Swf_krst_ov_g_big swf_krst_ov_g_big }

{ Swf_krst_mh_a swf_krst_mh_a }
{ Swf_krst_mh_b swf_krst_mh_b }
{ Swf_krst_mh_c swf_krst_mh_c }
{ Swf_krst_mh_d swf_krst_mh_d }
{ Swf_krst_mh_e swf_krst_mh_e }
{ Swf_krst_mh_g swf_krst_mh_g }
{ Swf_krst_mh_h swf_krst_mh_h }
{ Swf_krst_mh_i swf_krst_mh_i }
{ Swf_krst_mh_j swf_krst_mh_j }
{ Swf_krst_mh_k swf_krst_mh_k }
{ Swf_krst_mh_m swf_krst_mh_m }
{ Swf_krst_mh_p swf_krst_mh_p }
{ Swf_krst_mh_q swf_krst_mh_q }
{ Swf_krst_mh_r swf_krst_mh_r }
{ Swf_krst_mh_s swf_krst_mh_s }
{ Swf_krst_mh_t swf_krst_mh_t }
{ Swf_krst_mh_u swf_krst_mh_u }
{ Swf_krst_mh_v swf_krst_mh_v }
{ Swf_krst_mh_w swf_krst_mh_w }
{ Swf_krst_mh_x swf_krst_mh_x }
{ Swf_krst_mh_y swf_krst_mh_y }
{ Swf_krst_oh_a swf_krst_oh_a }
{ Swf_krst_oh_b swf_krst_oh_b }
{ Swf_krst_oh_c swf_krst_oh_c }
{ Swf_krst_oh_d swf_krst_oh_d }
{ Swf_krst_oh_e swf_krst_oh_e }
{ Swf_krst_uh_a swf_krst_uh_a }
{ Swf_krst_uh_b swf_krst_uh_b }
{ Swf_krst_uh_c swf_krst_uh_c }
{ Swf_krst_uh_d swf_krst_uh_d }
{ Swf_krst_uh_e swf_krst_uh_e }
{ Swf_krst_uh_f swf_krst_uh_f }
{ Titanic_bug titanic_bug }
{ Titanic_heck titanic_heck }
{ Titanic_rboot_a titanic_rboot_a }
// { Titanic_rboot_b titanic_rboot_b }
{ Titanic_rboot_c titanic_rboot_c }
{ Titanic_rboot_d titanic_rboot_d }
{ Titanic_rboot_e titanic_rboot_e }
{ Titanic_wand_a titanic_wand_a }

{ Titanic_wand_c titanic_wand_c }

{ Titanic_wand_f titanic_wand_f }


{ Titanic_wand_j titanic_wand_j }

{ Titanic_wand_l titanic_wand_l }

{ Titanic_wand_n titanic_wand_n }


{ Titanic_wand_q titanic_wand_q }




{ Titanic_wand_ac titanic_wand_ac }
{ Titanic_wand_ad titanic_wand_ad }
{ Titanic_wand_ae titanic_wand_ae }
{ Titanic_wand_af titanic_wand_af }
{ Titanic_wand_ag titanic_wand_ag }
{ Titanic_wand_ah titanic_wand_ah }
{ Titanic_wand_ai titanic_wand_ai }
{ Titanic_wand_aj titanic_wand_aj }
{ Titanic_wand_ak titanic_wand_ak }
{ Titanic_wand_al titanic_wand_al }
{ Titanic_wand_am titanic_wand_am }
// { Titanic_wand_an titanic_wand_an }
{ Titanic_wand_ao titanic_wand_ao }
{ Titanic_wand_ap titanic_wand_ap }
{ Titanic_wand_aq titanic_wand_aq }
// { Titanic_wand_ar titanic_wand_ar }
// { Titanic_wand_as titanic_wand_as }
// { Titanic_wand_at titanic_wand_at }



// { Titanic_wand_ax titanic_wand_ax }
// { Titanic_wand_ay titanic_wand_ay }
// { Titanic_wand_az titanic_wand_az }
{ Titanic_wand_ba titanic_wand_ba }
{ Titanic_wand_bb titanic_wand_bb }
// { Titanic_wand_bc titanic_wand_bc }
// { Titanic_wand_bd titanic_wand_bd }
// { Titanic_wand_be titanic_wand_be }
// { Titanic_wand_bf titanic_wand_bf }
// { Titanic_wand_bg titanic_wand_bg }
{ Titanic_rring titanic_rring }
{ Titanic_treppe titanic_treppe }

{ Titanic_sofa_a titanic_sofa_a }
{ Titanic_tisch_a titanic_tisch_a }
{ Titanic_tisch_b titanic_tisch_b }
{ Titanic_sessel_a titanic_sessel_a }

{ Swf_sta_vorne_u_a swf_sta_vorne_u_a }
{ Swf_sta_vorne_u_b swf_sta_vorne_u_b }
{ Swf_sta_vorne_u_c swf_sta_vorne_u_c }

{ Swf_sta_vorne_u_d swf_sta_vorne_u_d }
{ Swf_sta_vorne_u_e swf_sta_vorne_u_e }
{ Swf_sta_vorne_u_f swf_sta_vorne_u_f }
{ Swf_sta_vorne_u_g swf_sta_vorne_u_g }

{ Swf_sta_vorne_u_d_big swf_sta_vorne_u_d_big }
{ Swf_sta_vorne_u_e_big swf_sta_vorne_u_e_big }
{ Swf_sta_vorne_u_f_big swf_sta_vorne_u_f_big }
{ Swf_sta_vorne_u_g_big swf_sta_vorne_u_g_big }

{ Swf_sta_vorne_o_a swf_sta_vorne_o_a }
{ Swf_sta_vorne_o_b swf_sta_vorne_o_b }
{ Swf_sta_vorne_o_c swf_sta_vorne_o_c }
{ Swf_sta_vorne_o_d swf_sta_vorne_o_d }
{ Swf_sta_vorne_o_e swf_sta_vorne_o_e }
{ Swf_sta_vorne_o_f swf_sta_vorne_o_f }
{ Swf_sta_vorne_o_g swf_sta_vorne_o_g }
{ Swf_sta_vorne_o_h swf_sta_vorne_o_h }
{ Swf_sta_vorne_o_i swf_sta_vorne_o_i }
{ Swf_sta_vorne_o_j swf_sta_vorne_o_j }
{ Swf_sta_vorne_o_k swf_sta_vorne_o_k }
{ Swf_sta_vorne_o_l swf_sta_vorne_o_l }
{ Swf_sta_vorne_o_m swf_sta_vorne_o_m }
// { Swf_sta_vorne_o_n swf_sta_vorne_o_n }

{ Swf_sta_vorne_o_a_big swf_sta_vorne_o_a_big }
{ Swf_sta_vorne_o_b_big swf_sta_vorne_o_b_big }
// { Swf_sta_vorne_o_c_big swf_sta_vorne_o_c_big }
// { Swf_sta_vorne_o_d_big swf_sta_vorne_o_d_big }
{ Swf_sta_vorne_o_e_big swf_sta_vorne_o_e_big }
{ Swf_sta_vorne_o_f_big swf_sta_vorne_o_f_big }
{ Swf_sta_vorne_o_g_big swf_sta_vorne_o_g_big }
// { Swf_sta_vorne_o_h_big swf_sta_vorne_o_h_big }
// { Swf_sta_vorne_o_i_big swf_sta_vorne_o_i_big }
{ Swf_sta_vorne_o_j_big swf_sta_vorne_o_j_big }
// { Swf_sta_vorne_o_k_big swf_sta_vorne_o_k_big }
{ Swf_sta_vorne_o_l_big swf_sta_vorne_o_l_big }
{ Swf_sta_vorne_o_m_big swf_sta_vorne_o_m_big }
{ Swf_sta_vorne_o_n_big swf_sta_vorne_o_n_big }

{ Swf_sta_vorne_a swf_sta_vorne_a }
// { Swf_sta_vorne_b swf_sta_vorne_b }
{ Swf_sta_vorne_c swf_sta_vorne_c }
{ Swf_sta_vorne_d swf_sta_vorne_d }
{ Swf_sta_vorne_e swf_sta_vorne_e }
{ Swf_sta_vorne_f swf_sta_vorne_f }
{ Swf_sta_vorne_g swf_sta_vorne_g }
{ Titanic_sstein titanic_sstein }
{ Balken_titanic_a balken_titanic_a }
{ Balken_titanic_b balken_titanic_b }

// { Titanic_pumpe titanic_pumpe }
// { Titanic_kolben titanic_kolben }
{ Lore_kohlesilo lore_kohlesilo }
// { Achse_lore achse_lore }
// { Achse_zug achse_zug }

{ Knockers_turm_e knockers_turm_e }

{ Knockers_turm_g knockers_turm_g }
{ Knockers_turm_h knockers_turm_h }
{ Knockers_turm_i knockers_turm_i }

{ Knockers_turm_k knockers_turm_k }
{ Knockers_turm_l knockers_turm_l }
{ Hamsterfix hamsterfix }
{ Hamsterfix_b hamsterfix_b }
{ Hamsterfix_c hamsterfix_c }
{ Haken_a haken_a }
{ Haken_b haken_b }
// { Haken_c haken_c }
{ Haken_d haken_d }
// { Haken_e haken_e }
{ Haken_f haken_f }
{ Schiene_a schiene_a }
{ Schiene_a_b schiene_a_b }
{ Schiene_a_c schiene_a_c }

// { Schiene_a_e schiene_a_e }

{ Schiene_a_g schiene_a_g }
{ Schiene_b schiene_b }
{ Schiene_c schiene_c }
{ Schiene_d schiene_d }
{ Schiene_e schiene_e }
{ Schiene_f schiene_f }
{ Schiene_g schiene_g }
{ Schiene_h schiene_h }
{ Schiene_i schiene_i }
{ Schiene_j schiene_j }
{ Schiene_k schiene_k }

{ Schiene_m schiene_m }

{ Schiene_o schiene_o }

{ Schiene_q schiene_q }
{ Schiene_r schiene_r }
{ Schiene_s schiene_s }
{ Schiene_t schiene_t }
{ Schiene_u schiene_u }

{ Schiene_x schiene_x }
{ Schiene_y schiene_y }
{ Schiene_z schiene_z }
{ Schiene_ab schiene_ab }
{ Schiene_loop schiene_loop }
{ Stollenbahn_a stollenbahn_a }
{ Stollenbahn_b stollenbahn_b }
{ Stollenbahn_c stollenbahn_c }
{ Stollenbahn_d stollenbahn_d }
{ Stollenbahn_e stollenbahn_e }
{ Stollenbahn_f stollenbahn_f }
{ Stollenbahn_g stollenbahn_g }
{ Stollenbahn_h stollenbahn_h }
{ Stollenbahn_i stollenbahn_i }
{ Lore_stuetze_a lore_stuetze_a }
{ Lore_stuetze_b lore_stuetze_b }
{ Lore_stuetze_c lore_stuetze_c }
{ Lore_stuetze_d lore_stuetze_d }
{ Lore_stuetze_e lore_stuetze_e }
{ Lore_stuetze_f lore_stuetze_f }
{ Lore_stuetze_g lore_stuetze_g }
// { Lore_stuetze_h lore_stuetze_h }
{ Lore_stuetze_i lore_stuetze_i }
// { Lore_stuetze_j lore_stuetze_j }
{ Lore_stuetze_k lore_stuetze_k }
{ Lore_stopper_a lore_stopper_a }
{ Lore_stopper_b lore_stopper_b }
// { Lore_schalter_a lore_schalter_a }
// { Lore_schalter_b lore_schalter_b }
// { Lore_schalter_c lore_schalter_c }
// { Lore_schalter_d lore_schalter_d }
// { Lore_schalter_e lore_schalter_e }
{ Lore_bruecke_a lore_bruecke_a }
{ Lore_bruecke_b lore_bruecke_b }
{ Lore_bruecke_c lore_bruecke_c }
{ Lore_bruecke_d lore_bruecke_d }
{ Lore_bruecke_e lore_bruecke_e }
// { Lore_bruecke_f lore_bruecke_f }
{ Lore_bruecke_g lore_bruecke_g }
{ Lore_bruecke_g_b lore_bruecke_g_b }
{ Lore_bruecke_h lore_bruecke_h }
{ Lore_bruecke_i lore_bruecke_i }
{ Lore_bruecke_j lore_bruecke_j }
{ Schalter_dach schalter_dach }
// { Swf_bruecke_a swf_bruecke_a }
// { Met_schalter_a met_schalter_a }
// { Swf_eierbecher swf_eierbecher }
// { Swf_eierbecher_b swf_eierbecher_b }
// { Swf_ei swf_ei }
// { Zahnrad_a zahnrad_a }
{ Met_haufen_a met_haufen_a }
{ Met_haufen_b met_haufen_b }
{ Met_haufen_c met_haufen_c }
{ Met_haufen_d met_haufen_d }
{ Met_haufen_e met_haufen_e }
{ Met_haufen_f met_haufen_f }
{ Lore_schild_e lore_schild_e }
{ Lore_schild_f lore_schild_f }
{ Lore_schild_g lore_schild_g }
{ Lore_schild_h lore_schild_h }
{ Lore_schild_i lore_schild_i }
{ Lore_schild_j lore_schild_j }
// { Titanic_kronenleuchter titanic_kronenleuchter }
{ Wand_schwarz wand_schwarz }
{ Swf_bruecke_end_a swf_bruecke_end_a }
{ Swf_bruecke_end_b swf_bruecke_end_b }
{ Met_balken_a met_balken_a }

}




SetDummyClassesNoZ {

{ Boden_met_a boden_met_a }
{ Boden_met_b boden_met_b }
// { Boden_met_c boden_met_c }
{ Boden_met_d boden_met_d }
{ Boden_met_e boden_met_e }
{ Boden_met_f boden_met_f }
{ Boden_met_g boden_met_g }
{ Boden_met_h boden_met_h }
{ Boden_met_i boden_met_i }


{ Boden_met_end_a boden_met_end_a }
{ Boden_met_end_b boden_met_end_b }
{ Boden_met_end_c boden_met_end_c }
{ Boden_met_end_d boden_met_end_d }
{ Boden_met_end_e boden_met_end_e }

{ Titanic_boden_a titanic_boden_a }
{ Titanic_boden_b titanic_boden_b }
{ Titanic_boden_c titanic_boden_c }
{ Titanic_boden_d titanic_boden_d }
// { Titanic_boden_e titanic_boden_e }
{ Titanic_boden_f titanic_boden_f }
// { Titanic_boden_g titanic_boden_g }
// { Titanic_boden_h titanic_boden_h }
// { Titanic_boden_i titanic_boden_i }







}











