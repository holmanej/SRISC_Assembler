# SRISC Assembler Spec

### Mnemonics

```
imm		target[0..3]	value[d0..255]
						value[h0..ff]
						value[b.....]
add		target[0..3]	a[0..3]		b[0..3]
sub		target[0..3]	a[0..3]		b[0..3]
and		target[0..3]	a[0..3]		b[0..3]
or		target[0..3]	a[0..3]		b[0..3]
xor		target[0..3]	a[0..3]		b[0..3]
inv		target[0..3]	a[0..3]
sll		target[0..3]	a[0..3]		b[0..3]
srl		target[0..3]	a[0..3]		b[0..3]
inc		a[0..3]
dec		a[0..3]
ind		a[0..3]
ult		a[0..3]		b[0..3]
slt		a[0..3]		b[0..3]
equ		a[0..3]		b[0..3]
eqz		a[0..3]
set
load	target[0..3]	addr[0..110]
load	target[0..3]
store	source[0..3]	addr[0..110]
store	source[0..3]
read	target[0..3]	addr[0..15]
write	source[0..3]	addr[0..15]
br		addr[0..1023]
```
		
## Compilation

### Lexicon

```
<mnemonics>
<type>
	byte
	short
	int
	long
	
if
for
while
```


#### Variables (byte)
* variable list
* swaps most oldest modified register
* init done first, optional assign after
##### Syntax
```
		byte a
		a++
		a--
		a ? #
		a ? b
		(byte) a = #
		(byte) a = b
		(byte) a = ~b
		(byte) a = # @ #
		(byte) a = b @ #
		(byte) a = b @ c
```
#### Ifs
* code inside placed elsewhere
* true branches to code and jumps back
* false continues in place
##### Syntax
```
		if (a ? #/b)
		end
```
		
##### Procedure
```
      if ? is '=' and #/b is 0
      else
          if #/b is #
              imm #
          else if op2 exists
              imm value
          else
              error - var not found
          end		
      end
      compare
      branch?
      run
      return
      delete vars inside
```

#### For
* code inside placed elsewhere
* immediately jump to conditional
* true branches to top of loop
* false continues to jump back
##### Syntax
```
		for (<type> a = #/b; a ? #/c; a @ #/d)
		end
```	
  ##### Procedure
```
      init? a
      imm #/b
      jump to conditional
          compare
          branch? to top of loop
          run
          imm #/d
          modify a
      jump back
```

#### While
* code inside placed elsewhere
* immediately jump to conditional
* true branches to top of loop
* false continues to jump back
##### Syntax
```
		while (a @ #/b)
		end
```	
##### Procedure
```
      imm a
      imm #/b
      jump to conditional
          compare
          branch? to top of loop
          run
      jump back
```

#### Arrays
	shorts/ints/longs
		stored big endian
	strings
	linked lists
	
### Ideas
* registers cache memory caches SD card
* self-reprogram from SD card
* run linux