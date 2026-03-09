import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { DRACOLoader } from 'three/addons/loaders/DRACOLoader.js';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';

let scene, camera, renderer, model;
let composer, bloomPass;
let container;
let mixer, actions = {};
let isInitialized = false;
let currentLoadingId = 0; // Loading Session ID

// Particle System
let particles = [];
const particleCount = 20;

const dracoLoader = new DRACOLoader();
dracoLoader.setDecoderPath('https://www.gstatic.com/draco/versioned/decoders/1.5.6/');

const loader = new GLTFLoader();
loader.setDRACOLoader(dracoLoader);

export function initPetScene(canvasId) {
    const newContainer = document.getElementById(canvasId);
    if (!newContainer) return;

    // The Root-Cause Fix 7: Re-attachment Logic
    // Nếu đã khởi tạo nhưng container bị thay đổi (do Blazor render lại DOM), gắn lại canvas vào container mới
    if (isInitialized) {
        if (container !== newContainer) {
            console.log("Re-attaching 3D Scene to new container:", canvasId);
            container = newContainer;
            if (renderer && renderer.domElement) {
                container.appendChild(renderer.domElement);
                onWindowResize(); // Cập nhật lại kích thước ngay lập tức
            }
        }
        return;
    }

    container = newContainer;
    const rect = container.getBoundingClientRect();
    // Nếu chưa có kích thước (đang ẩn), đợi resize tiếp theo hoặc khởi tạo sau
    if (rect.width === 0 || rect.height === 0) {
        console.warn("Container has zero size, waiting for resize...");
        window.addEventListener('resize', onWindowResize, { once: true });
        return;
    }

    scene = new THREE.Scene();

    camera = new THREE.PerspectiveCamera(45, rect.width / rect.height, 0.1, 1000);
    camera.position.set(0, 1, 3);

    renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: "high-performance" });
    renderer.setSize(rect.width, rect.height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)); // Giới hạn pixelRatio để tối ưu hiệu năng
    renderer.setClearColor(0x000000, 0); // Đảm bảo nền trong suốt tuyệt đối
    renderer.toneMapping = THREE.ACESFilmicToneMapping; // Chuyển sang ToneMapping chuẩn HDR hơn
    renderer.toneMappingExposure = 1.2;
    container.appendChild(renderer.domElement);

    const renderScene = new RenderPass(scene, camera);
    const renderTarget = new THREE.WebGLRenderTarget(rect.width, rect.height, {
        minFilter: THREE.LinearFilter,
        magFilter: THREE.LinearFilter,
        format: THREE.RGBAFormat,
        type: THREE.HalfFloatType
    });

    composer = new EffectComposer(renderer, renderTarget);
    composer.addPass(renderScene);

    // UnrealBloomPass can kill alpha, we need a custom approach or lower strength
    bloomPass = new UnrealBloomPass(new THREE.Vector2(rect.width, rect.height), 1.5, 0.4, 0.85); // Initialize bloomPass here
    bloomPass.strength = 1.2;
    bloomPass.radius = 0.8;
    bloomPass.threshold = 0.2;
    composer.addPass(bloomPass);

    // Add a final pass to ensure alpha is handled correctly if needed, 
    // but ACESFilmic + RGBAFormat usually works with 0 clear alpha.

    scene.add(new THREE.AmbientLight(0xffffff, 1.2));
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(5, 5, 5);
    scene.add(directionalLight);

    const clock = new THREE.Clock();

    function animate() {
        requestAnimationFrame(animate);
        const delta = clock.getDelta();

        // Guard: Chỉ render nếu có kích thước hợp lệ để tránh lỗi Framebuffer
        if (container.clientWidth === 0 || container.clientHeight === 0) return;

        if (mixer) mixer.update(delta);
        if (model && !mixer) model.rotation.y += 0.5 * delta;

        for (let i = particles.length - 1; i >= 0; i--) {
            const p = particles[i];
            p.position.addScaledVector(p.velocity, delta);
            p.material.opacity -= delta * 0.8;
            if (p.material.opacity <= 0) {
                scene.remove(p);
                p.geometry.dispose();
                p.material.dispose();
                particles.splice(i, 1);
            }
        }

        composer.render();
    }
    animate();

    window.addEventListener('resize', onWindowResize);
    container.addEventListener('click', () => triggerDopamine());
    isInitialized = true;
    console.log("Pet Scene Hardened & Animated");
}

function onWindowResize() {
    if (!container) return;

    const rect = container.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) return;

    if (!isInitialized) {
        initPetScene(container.id);
        return;
    }

    if (!camera || !renderer || !composer) return;

    camera.aspect = rect.width / rect.height;
    camera.updateProjectionMatrix();
    renderer.setSize(rect.width, rect.height);
    composer.setSize(rect.width, rect.height);
}

// Hàm giải phóng bộ nhớ GPU triệt để (The Root-Cause Fix 1)
function disposeObject(obj) {
    if (!obj) return;
    obj.traverse((node) => {
        if (node.isMesh) {
            if (node.geometry) node.geometry.dispose();
            if (node.material) {
                if (Array.isArray(node.material)) {
                    node.material.forEach(m => disposeMaterial(m));
                } else {
                    disposeMaterial(node.material);
                }
            }
        }
    });
}

function disposeMaterial(mat) {
    for (const key in mat) {
        if (mat[key] && mat[key].isTexture) {
            mat[key].dispose();
        }
    }
    mat.dispose();
}

// Procedural Fallback: Bonsai Thiền Định (The Zen Bonsai)
function createFallbackModel() {
    const group = new THREE.Group();

    // 1. Chậu gốm nghệ thuật (Torus + Cylinder)
    const potGroup = new THREE.Group();
    const potGeo = new THREE.CylinderGeometry(0.4, 0.3, 0.15, 24);
    const potMat = new THREE.MeshStandardMaterial({
        color: 0x2c3e50,
        roughness: 0.2,
        metalness: 0.5
    });
    const pot = new THREE.Mesh(potGeo, potMat);
    potGroup.add(pot);

    const rimGeo = new THREE.TorusGeometry(0.4, 0.03, 12, 24);
    const rim = new THREE.Mesh(rimGeo, potMat);
    rim.rotation.x = Math.PI / 2;
    rim.position.y = 0.075;
    potGroup.add(rim);

    potGroup.position.y = 0.075;
    group.add(potGroup);

    // 2. Thân cây uốn lượn (Curve path with tube)
    const points = [];
    points.push(new THREE.Vector3(0, 0, 0));
    points.push(new THREE.Vector3(0.1, 0.2, 0.05));
    points.push(new THREE.Vector3(-0.1, 0.5, -0.05));
    points.push(new THREE.Vector3(0.2, 0.8, 0.1));

    const curve = new THREE.CatmullRomCurve3(points);
    const trunkGeo = new THREE.TubeGeometry(curve, 20, 0.05, 8, false);
    const trunkMat = new THREE.MeshStandardMaterial({
        color: 0x4e342e,
        roughness: 0.9
    });
    const trunk = new THREE.Mesh(trunkGeo, trunkMat);
    trunk.position.y = 0.15;
    group.add(trunk);

    // 3. Tán lá (Low-poly clouds of focus)
    const leafGeo = new THREE.IcosahedronGeometry(1, 0);
    const leaves = new THREE.Group();

    const leafPositions = [
        { pos: [0.2, 0.8, 0.1], scale: 0.2, color: 0x81c784 },
        { pos: [0.35, 0.75, 0.15], scale: 0.15, color: 0x66bb6a },
        { pos: [0.1, 0.85, 0.05], scale: 0.18, color: 0xa5d6a7 }
    ];

    leafPositions.forEach(config => {
        const mat = new THREE.MeshStandardMaterial({
            color: config.color,
            flatShading: true,
            transparent: true,
            opacity: 0.85
        });
        const leaf = new THREE.Mesh(leafGeo, mat);
        leaf.position.set(...config.pos);
        leaf.scale.setScalar(config.scale);
        leaves.add(leaf);
    });
    leaves.position.y = 0.15; // Offset trunk position
    group.add(leaves);

    // 4. Quả cầu linh hồn (Spiritual core)
    const soulGeo = new THREE.SphereGeometry(0.15, 16, 16);
    const soulMat = new THREE.MeshStandardMaterial({
        color: 0x64ffda,
        emissive: 0x64ffda,
        emissiveIntensity: 4.0,
        transparent: true,
        opacity: 0.95
    });
    const soul = new THREE.Mesh(soulGeo, soulMat);
    soul.position.set(0.2, 1.1, 0.1);
    group.add(soul);

    // 5. Cánh sen năng lượng (Floating petals)
    const petalGeo = new THREE.ConeGeometry(0.04, 0.12, 3);
    const petals = new THREE.Group();
    for (let i = 0; i < 5; i++) {
        const petal = new THREE.Mesh(petalGeo, soulMat);
        const angle = (i / 5) * Math.PI * 2;
        petal.position.set(Math.sin(angle) * 0.25, 0, Math.cos(angle) * 0.25);
        petal.rotation.x = Math.PI / 2;
        petal.rotation.z = angle;
        petals.add(petal);
    }
    petals.position.set(0.2, 1.1, 0.1);
    group.add(petals);

    // Animation
    const startTime = Date.now();
    group.onBeforeRender = () => {
        const time = (Date.now() - startTime) * 0.001;

        // Soul pulse
        soul.scale.setScalar(1 + Math.sin(time * 3) * 0.1);
        soul.position.y = 1.1 + Math.sin(time * 2) * 0.05;

        // Petals rotation
        petals.rotation.y += 0.02;
        petals.position.y = soul.position.y;

        // Swaying trunk
        trunk.rotation.z = Math.sin(time) * 0.02;
        leaves.rotation.z = Math.sin(time) * 0.03;

        // Rotating leaves for shimmer
        leaves.children.forEach((l, i) => {
            l.rotation.y += 0.01 * (i + 1);
        });
    };

    return group;
}

export async function loadPetModel(modelUrl, containerId) {
    if (!scene) {
        console.log("Scene not initialized, attempting late init for:", containerId);
        initPetScene(containerId);
    }

    if (!scene) return { success: false, error: "Scene not initialized after attempt" };

    const sessionId = ++currentLoadingId; // Increment session (The Root-Cause Fix 2)

    return new Promise((resolve) => {
        loader.load(modelUrl, (gltf) => {
            // Kiểm tra xem yêu cầu này có còn mới nhất không
            if (sessionId !== currentLoadingId) {
                console.warn("Discarding outdated model load:", modelUrl);
                disposeObject(gltf.scene);
                return resolve({ success: false, error: "Outdated request" });
            }

            if (model) {
                scene.remove(model);
                disposeObject(model);
            }

            model = gltf.scene;

            // Setup Animations
            mixer = new THREE.AnimationMixer(model);
            actions = {};
            gltf.animations.forEach(clip => {
                const name = clip.name.toLowerCase();
                if (name.includes('idle')) actions['idle'] = mixer.clipAction(clip);
                if (name.includes('happy') || name.includes('jump')) actions['happy'] = mixer.clipAction(clip);
            });

            if (actions['idle']) actions['idle'].play();

            const box = new THREE.Box3().setFromObject(model);
            const center = box.getCenter(new THREE.Vector3());
            model.position.sub(center);
            scene.add(model);

            resolve({ success: true });
        }, undefined, (error) => {
            console.warn("3D Asset not found or error, using procedural fallback:", modelUrl);

            if (model) {
                scene.remove(model);
                disposeObject(model);
            }

            model = createFallbackModel();
            scene.add(model);

            // Resolve success even on fallback so UI doesn't show 404 error
            resolve({ success: true, isFallback: true });
        });
    });
}

export function triggerDopamine() {
    if (!scene) return;

    // 1. Play Happy Animation
    if (actions['happy']) {
        const happy = actions['happy'];
        happy.reset().setLoop(THREE.LoopOnce).play();
        happy.clampWhenFinished = true;

        const onFinished = () => {
            mixer.removeEventListener('finished', onFinished);
            if (actions['idle']) actions['idle'].play();
        };
        mixer.addEventListener('finished', onFinished);
    }

    // 2. Visual Effects
    const originalStrength = bloomPass.strength;
    bloomPass.strength = 2.0;
    setTimeout(() => { bloomPass.strength = originalStrength; }, 500);

    const geometry = new THREE.SphereGeometry(0.05, 8, 8);
    const colors = [0xFFD700, 0xFFFFFF, 0x00FF00];

    for (let i = 0; i < particleCount; i++) {
        const material = new THREE.MeshBasicMaterial({
            color: colors[Math.floor(Math.random() * colors.length)],
            transparent: true,
            opacity: 1.0
        });
        const p = new THREE.Mesh(geometry, material);
        const modelPos = model ? model.position : new THREE.Vector3(0, 0.5, 0);
        p.position.copy(modelPos).add(new THREE.Vector3(0, 0.5, 0));
        p.velocity = new THREE.Vector3((Math.random() - 0.5) * 4, (Math.random() - 0.5) * 4 + 2, (Math.random() - 0.5) * 4);
        particles.push(p);
        scene.add(p);
    }
}

window.petRenderer = {
    init: (id) => initPetScene(id),
    loadModel: (url, containerId) => loadPetModel(url, containerId),
    triggerDopamine: () => triggerDopamine()
};
