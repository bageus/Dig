//Ringe.tcl


// ACHTUNG: Der Ring der Magie befindet sich "for technical reasons" in Lorelei.tcl! - David


def_class Ring_Des_Lebens none tool 0 {} {
	class_defaultanim naturring.standard
	obj_init {
		set_physic this 1
		set_viewinfog this 1

		call_method this reset
	}

	method setstandardanim {} {
		set_anim this naturring.standard 0 2
	}

	method reset {} {
		set_anim this naturring.drehen 0 2
//		change_particlesource this 0 13 {0 -0.4 0.1} {0 0 0} 32 1 0
		set_particlesource this 0 1
	}
}

def_class Ring_Des_Wassers none tool 0 {} {
	class_defaultanim wasserring.standard
	obj_init {
		set_physic this 1
		set_viewinfog this 1

		call_method this reset
	}

	method setstandardanim {} {
		set_anim this wasserring.standard 0 2
		change_particlesource this 0 21 {0 0 0} {0 0 0} 64 2 0
		set_particlesource this 0 1
	}

	method reset {} {
		set_anim this wasserring.drehen 0 2
		change_particlesource this 0 21 {0 0 0} {0 0 0} 64 2 0
		set_particlesource this 0 1
	}
}


def_class Ring_Des_Feuers none tool 0 {} {
	class_defaultanim feuerring.standard
	obj_init {
		set_physic this 1
		set_viewinfog this 1

		call_method this reset
	}

	method setstandardanim {} {
		set_anim this feuerring.standard 0 2
	}
	
	method reset {} {
		set_anim this feuerring.drehen 0 2
		change_particlesource this 0 27 {0 0 0} {0 0 0} 64 2 0 0 0 1
		set_particlesource this 0 1
	}
}


def_class Ring_Der_Luft none tool 0 {} {
	class_defaultanim luftring.standard
	obj_init {
		set_physic this 1
		set_viewinfog this 1

		call_method this reset
	}
	
	method setstandardanim {} {
		set_anim this luftring.standard 0 2
		change_particlesource this 0 11 {0 0 0} {0 0 0} 32 1 0
		set_particlesource this 0 1
	}
	
	method reset {} {
		set_anim this luftring.drehen 0 2
		change_particlesource this 0 11 {0 0 0} {0 0 0} 32 1 0
		set_particlesource this 0 1
	}
}

def_class Ring_Der_Erde none tool 0 {} {
	class_defaultanim erdring.standard
	obj_init {
		set_physic this 1
		set_viewinfog this 1

		call_method this reset
	}

	method setstandardanim {} {
		set_anim this erdring.standard 0 2
	}

	method reset {} {
		set_anim this erdring.drehen 0 2
//		change_particlesource this 0 13 {0 -0.4 0.1} {0 0 0} 32 1 0
		set_particlesource this 0 1
	}
}
