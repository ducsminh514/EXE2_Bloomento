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

    renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true,
        powerPreference: "high-performance",
        premultipliedAlpha: false,
        preserveDrawingBuffer: false
    });
    renderer.setSize(rect.width, rect.height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setClearColor(0x000000, 0); // Đưa về Black 0 Alpha - chuẩn an toàn nhất của WebGL
    renderer.setClearAlpha(0);
    renderer.domElement.style.setProperty('background', 'transparent', 'important');
    renderer.domElement.style.outline = 'none';
    scene.background = null;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.2;
    container.appendChild(renderer.domElement);

    // Initial check for post-processing
    composer = null;
    renderer.autoClear = true;

    scene.add(new THREE.AmbientLight(0xffffff, 1.2));
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(5, 5, 5);
    scene.add(directionalLight);

    const clock = new THREE.Clock();

    function animate() {
        requestAnimationFrame(animate);
        const delta = clock.getDelta();

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

        if (composer) {
            composer.render();
        } else {
            renderer.render(scene, camera);
        }
    }
    animate();

    window.addEventListener('resize', onWindowResize);
    container.addEventListener('click', () => triggerDopamine());
    isInitialized = true;
    console.log("Pet Scene Re-initialized with transparency");
}

function onWindowResize() {
    if (!container) return;

    const rect = container.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) return;

    if (!isInitialized) {
        initPetScene(container.id);
        return;
    }

    if (!camera || !renderer) return;

    camera.aspect = rect.width / rect.height;
    camera.updateProjectionMatrix();
    renderer.setSize(rect.width, rect.height);
    if (composer) composer.setSize(rect.width, rect.height);
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

    // 1. Hòn đảo lơ lửng (Floating Island) thay vì chậu gốm
    const islandGroup = new THREE.Group();

    // Geometry cho đảo (Low-poly rock)
    const islandGeo = new THREE.IcosahedronGeometry(0.5, 1);
    const islandMat = new THREE.MeshStandardMaterial({
        color: 0x3d4444, // Slate Rock
        flatShading: true,
        roughness: 0.8,
        metalness: 0.2
    });
    const island = new THREE.Mesh(islandGeo, islandMat);
    island.scale.set(1.2, 0.4, 1.2);
    island.position.y = 0.1;
    islandGroup.add(island);

    // Thêm một lớp cỏ phía trên
    const grassGeo = new THREE.CylinderGeometry(0.55, 0.5, 0.1, 8);
    const grassMat = new THREE.MeshStandardMaterial({
        color: 0x4caf50,
        flatShading: true
    });
    const grass = new THREE.Mesh(grassGeo, grassMat);
    grass.position.y = 0.25;
    islandGroup.add(grass);

    group.add(islandGroup);

    // 2. Thân cây hữu cơ (Organic Trunk) - Thon dần về phía ngọn
    const trunkPoints = [
        new THREE.Vector3(0, 0, 0),
        new THREE.Vector3(0.05, 0.2, 0.05),
        new THREE.Vector3(-0.05, 0.5, 0.1),
        new THREE.Vector3(0.1, 0.8, -0.05),
        new THREE.Vector3(0, 1.1, 0)
    ];
    const trunkCurve = new THREE.CatmullRomCurve3(trunkPoints);
    const trunkGeo = new THREE.TubeGeometry(trunkCurve, 20, 0.06, 8, false);

    // Tùy chỉnh độ dày thon dần (Tapering) bàng cách can thiệp vào vertices (đơn giản hóa bằng scale)
    const trunkMat = new THREE.MeshStandardMaterial({
        color: 0x4a3728, // Dark Wood
        roughness: 0.9,
        metalness: 0.1
    });
    const trunk = new THREE.Mesh(trunkGeo, trunkMat);
    group.add(trunk);

    // 3. Cành phụ (Branches)
    const branchMat = trunkMat.clone();
    const createBranch = (start, end, radius) => {
        const curve = new THREE.LineCurve3(start, end);
        const geo = new THREE.TubeGeometry(curve, 8, radius, 6, false);
        return new THREE.Mesh(geo, branchMat);
    };

    const branch1 = createBranch(new THREE.Vector3(-0.02, 0.55, 0.08), new THREE.Vector3(-0.25, 0.7, 0.15), 0.03);
    const branch2 = createBranch(new THREE.Vector3(0.08, 0.75, -0.02), new THREE.Vector3(0.3, 0.85, 0.05), 0.025);
    group.add(branch1, branch2);

    // 4. Tán lá (Low-poly clouds of focus)
    const leafGeo = new THREE.IcosahedronGeometry(1, 0);
    const leaves = new THREE.Group();

    const leafPositions = [
        { pos: [0, 1.1, 0], scale: 0.3, color: 0x64ffda }, // Top cluster
        { pos: [-0.25, 0.75, 0.15], scale: 0.22, color: 0x1de9b6 }, // Left branch
        { pos: [0.3, 0.88, 0.05], scale: 0.2, color: 0x00bfa5 }, // Right branch
        { pos: [0.1, 0.95, -0.05], scale: 0.25, color: 0x64ffda } // Middle cluster
    ];

    leafPositions.forEach(config => {
        const cluster = new THREE.Group();
        // Một cụm gồm nhiều khối cầu tán lá (Clouds style)
        for (let i = 0; i < 3; i++) {
            const mat = new THREE.MeshPhysicalMaterial({
                color: config.color,
                flatShading: true,
                transparent: true,
                opacity: 0.6,
                transmission: 0.6,
                thickness: 0.5,
                roughness: 0.2,
                emissive: config.color,
                emissiveIntensity: 0.15
            });
            const subLeaf = new THREE.Mesh(leafGeo, mat);
            subLeaf.position.set(
                (Math.random() - 0.5) * 0.15,
                (Math.random() - 0.5) * 0.15,
                (Math.random() - 0.5) * 0.15
            );
            subLeaf.scale.setScalar(config.scale * (0.8 + Math.random() * 0.4));
            cluster.add(subLeaf);
        }
        cluster.position.set(...config.pos);
        leaves.add(cluster);
    });
    group.add(leaves);

    // 5. Quả cầu linh hồn (Soul Core)
    const coreGeo = new THREE.SphereGeometry(0.12, 16, 16);
    const coreMat = new THREE.MeshBasicMaterial({
        color: 0xffffff,
        transparent: true,
        opacity: 0.9
    });
    const core = new THREE.Mesh(coreGeo, coreMat);

    const coreGlowGeo = new THREE.SphereGeometry(0.25, 16, 16);
    const coreGlowMat = new THREE.MeshBasicMaterial({
        color: 0x64ffda,
        transparent: true,
        opacity: 0.3
    });
    const coreGlow = new THREE.Mesh(coreGlowGeo, coreGlowMat);

    const petals = new THREE.Group();
    petals.add(core);
    petals.add(coreGlow);
    petals.position.set(0, 1.15, 0);
    group.add(petals);

    // 6. Hiệu ứng Fireflies (Đom đóm Zen)
    const firefliesCount = 8;
    const fireflies = new THREE.Group();
    const fireflyGeo = new THREE.SphereGeometry(0.02, 4, 4);
    const fireflyMat = new THREE.MeshBasicMaterial({ color: 0x64ffda });

    for (let i = 0; i < firefliesCount; i++) {
        const firefly = new THREE.Mesh(fireflyGeo, fireflyMat);
        firefly.position.set(
            (Math.random() - 0.5) * 2,
            Math.random() * 1.5,
            (Math.random() - 0.5) * 2
        );
        firefly.userData = {
            speed: 0.3 + Math.random() * 0.5,
            offset: Math.random() * Math.PI * 2
        };
        fireflies.add(firefly);
    }
    group.add(fireflies);

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

        // Breathing foliage
        leaves.children.forEach((cluster, i) => {
            cluster.scale.setScalar(1 + Math.sin(time * 1.5 + i) * 0.05);
            cluster.rotation.y += 0.005 * (i + 1);
        });

        // Fireflies floating
        const range = 1.2;
        fireflies.children.forEach((f, i) => {
            f.position.y += Math.sin(time * f.userData.speed + f.userData.offset) * 0.008;
            f.position.x += Math.cos(time * 0.6 + f.userData.offset) * 0.004;
            f.position.z += Math.sin(time * 0.4 + f.userData.offset) * 0.004;

            // Re-center fireflies if they drift too far
            if (Math.abs(f.position.x) > range) f.position.x *= 0.9;
            if (Math.abs(f.position.z) > range) f.position.z *= 0.9;

            f.material.opacity = 0.3 + Math.abs(Math.sin(time * 2 + f.userData.offset)) * 0.7;
        });

        // Island floating
        islandGroup.position.y = Math.sin(time * 0.8) * 0.06;

        // Soul core pulse
        coreGlow.scale.setScalar(1 + Math.sin(time * 3) * 0.2);
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

    // 2. Visual Effects (Particles only for now to ensure alpha transparency)
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
